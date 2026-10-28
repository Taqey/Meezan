using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.Scraping.Processing;

/// <summary>
/// Executes an atomic bulk swap of all scraped live market tables inside
/// a single database transaction.
/// 
/// If any part of the operation fails, the transaction is rolled back,
/// leaving the existing live data completely intact and visible to reads.
/// </summary>
public class ScrapeBatchCommitter : IScrapeBatchCommitter
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ScrapeBatchCommitter> _logger;

    public ScrapeBatchCommitter(ApplicationDbContext context, ILogger<ScrapeBatchCommitter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CommitLiveAsync(IReadOnlyList<StockScrapedSnapshot> batch, CancellationToken ct = default)
    {
        if (batch == null || batch.Count == 0)
        {
            _logger.LogWarning("ScrapeBatchCommitter: Live batch is empty. Skipping commit.");
            return;
        }

        _logger.LogInformation("Beginning live market-data commit for {Count} stock snapshots...", batch.Count);

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                _context.ChangeTracker.Clear();

                // Fair-value snapshots are recalculated from persisted slow data plus the latest close.
                await _context.StockFairValueMethods.ExecuteDeleteAsync(ct);
                await _context.StockFairValues.ExecuteDeleteAsync(ct);
                await _context.StockSupportResistance.ExecuteDeleteAsync(ct);

                var fairValueList = new List<StockFairValue>();
                var supportResistanceList = new List<StockSupportResistance>();

                foreach (var snapshot in batch)
                {
                    if (snapshot.MarketData is not null)
                    {
                        await UpsertLiveMarketDataAsync(snapshot.MarketData, ct);
                    }

                    if (snapshot.FairValue is not null)
                    {
                        if (snapshot.FairValueMethods is { Count: > 0 })
                        {
                            snapshot.FairValue.Methods = snapshot.FairValueMethods;
                        }
                        fairValueList.Add(snapshot.FairValue);
                    }

                    if (snapshot.SupportResistance is not null)
                    {
                        supportResistanceList.Add(snapshot.SupportResistance);
                    }
                }

                if (fairValueList.Count > 0)
                    await _context.StockFairValues.AddRangeAsync(fairValueList, ct);

                if (supportResistanceList.Count > 0)
                    await _context.StockSupportResistance.AddRangeAsync(supportResistanceList, ct);

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "Live commit succeeded. Updated: {MD} MarketData, inserted: {FV} FairValues, {SR} SupportResistance.",
                    batch.Count(b => b.MarketData is not null), fairValueList.Count, supportResistanceList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed during live scrape commit. Rolling back changes.");
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task CommitSelectedLiveAsync(IReadOnlyList<StockScrapedSnapshot> batch, CancellationToken ct = default)
    {
        if (batch == null || batch.Count == 0)
        {
            _logger.LogWarning("ScrapeBatchCommitter: Selected batch is empty. Skipping commit.");
            return;
        }

        var stockIds = batch.Select(s => s.StockId).Distinct().ToList();
        _logger.LogInformation("Beginning selective live commit for {Count} stock(s)...", stockIds.Count);

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // ── Retry-safety ────────────────────────────────────────────────────
            // ExecuteAsync can replay the entire lambda on transient SQL failures.
            // On a retry, StockFairValue / StockFairValueMethod / StockSupportResistance
            // objects from the previous (failed) attempt may still be tracked in EF's
            // ChangeTracker (state Added). AddRangeAsync on the retry then throws because
            // they are already tracked. Fix: detach just those entity types so each
            // attempt starts clean for them, while leaving the caller's Stock and
            // ScrapeRunLog entities tracked (a full Clear() would break the outer handler).
            foreach (var entry in _context.ChangeTracker.Entries<StockFairValue>().ToList())
                entry.State = EntityState.Detached;
            foreach (var entry in _context.ChangeTracker.Entries<StockFairValueMethod>().ToList())
                entry.State = EntityState.Detached;
            foreach (var entry in _context.ChangeTracker.Entries<StockSupportResistance>().ToList())
                entry.State = EntityState.Detached;
            // ────────────────────────────────────────────────────────────────────

            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                // Scoped deletes — ONLY the selected stocks' rows, never the whole table.
                // ExecuteDelete bypasses the change tracker so these are safe on retry.
                // (Two-step for methods: ExecuteDelete cannot join via navigation.)
                var existingFvIds = await _context.StockFairValues
                    .Where(f => stockIds.Contains(f.StockId))
                    .Select(f => f.Id)
                    .ToListAsync(ct);
                if (existingFvIds.Count > 0)
                {
                    await _context.StockFairValueMethods
                        .Where(m => existingFvIds.Contains(m.StockFairValueId))
                        .ExecuteDeleteAsync(ct);
                }
                await _context.StockFairValues
                    .Where(f => stockIds.Contains(f.StockId))
                    .ExecuteDeleteAsync(ct);
                await _context.StockSupportResistance
                    .Where(s => stockIds.Contains(s.StockId))
                    .ExecuteDeleteAsync(ct);

                var fairValueList = new List<StockFairValue>();
                var supportResistanceList = new List<StockSupportResistance>();

                foreach (var snapshot in batch)
                {
                    if (snapshot.MarketData is not null)
                    {
                        await UpsertLiveMarketDataAsync(snapshot.MarketData, ct);
                    }

                    if (snapshot.FairValue is not null)
                    {
                        // Reset PK so EF always does an INSERT (the DB row was just
                        // deleted above, and Id may be non-zero from a previous attempt).
                        snapshot.FairValue.Id = 0;
                        snapshot.FairValue.Methods.Clear();
                        if (snapshot.FairValueMethods is { Count: > 0 })
                        {
                            foreach (var m in snapshot.FairValueMethods)
                            {
                                m.Id = 0;
                                m.StockFairValueId = 0;
                            }
                            snapshot.FairValue.Methods = snapshot.FairValueMethods;
                        }
                        fairValueList.Add(snapshot.FairValue);
                    }

                    if (snapshot.SupportResistance is not null)
                    {
                        // Reset PK so EF always does an INSERT.
                        snapshot.SupportResistance.Id = 0;
                        supportResistanceList.Add(snapshot.SupportResistance);
                    }
                }

                if (fairValueList.Count > 0)
                    await _context.StockFairValues.AddRangeAsync(fairValueList, ct);

                if (supportResistanceList.Count > 0)
                    await _context.StockSupportResistance.AddRangeAsync(supportResistanceList, ct);

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "Selective live commit succeeded for {Count} stock(s).",
                    stockIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed during selective live commit. Rolling back changes.");
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task CommitSlowAsync(IReadOnlyList<StockScrapedSnapshot> batch, CancellationToken ct = default)
    {
        if (batch == null || batch.Count == 0)
        {
            _logger.LogWarning("ScrapeBatchCommitter: Slow batch is empty. Skipping commit.");
            return;
        }

        _logger.LogInformation("Beginning slow market-data commit for {Count} stock snapshots...", batch.Count);

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                _context.ChangeTracker.Clear();

                foreach (var snapshot in batch)
                {
                    if (snapshot.MarketData is not null)
                    {
                        await UpsertSlowMarketDataAsync(snapshot.MarketData, ct);
                    }
                }

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "Slow commit succeeded. Updated {Count} MarketData rows.",
                    batch.Count(b => b.MarketData is not null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed during slow scrape commit. Rolling back changes.");
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private async Task UpsertLiveMarketDataAsync(StockMarketData data, CancellationToken ct)
    {
        var existing = await _context.StockMarketData
            .FirstOrDefaultAsync(m => m.StockId == data.StockId, ct);

        if (existing is null)
        {
            await _context.StockMarketData.AddAsync(data, ct);
            return;
        }

        existing.High = data.High;
        existing.Low = data.Low;
        existing.Open = data.Open;
        existing.ClosingPrice = data.ClosingPrice;
        // Daily market cap recomputation: last price * shares outstanding
        if (existing.ClosingPrice.HasValue && existing.SharesOutstanding.HasValue && existing.SharesOutstanding.Value > 0)
        {
            existing.MarketValue = existing.ClosingPrice.Value * existing.SharesOutstanding.Value;
        }
        else if (data.MarketValue.HasValue)
        {
            existing.MarketValue = data.MarketValue;
        }
        existing.SourceLastUpdateText = data.SourceLastUpdateText;
        existing.FetchedAt = data.FetchedAt;
    }

    private async Task UpsertSlowMarketDataAsync(StockMarketData data, CancellationToken ct)
    {
        var existing = await _context.StockMarketData
            .FirstOrDefaultAsync(m => m.StockId == data.StockId, ct);

        if (existing is null)
        {
            await _context.StockMarketData.AddAsync(data, ct);
            return;
        }

        existing.NominalValue = data.NominalValue;
        if (data.SharesOutstanding.HasValue)
        {
            existing.SharesOutstanding = data.SharesOutstanding;
        }
        // If shares outstanding and closing price exist, recalculate fresh market cap
        if (existing.ClosingPrice.HasValue && existing.SharesOutstanding.HasValue && existing.SharesOutstanding.Value > 0)
        {
            existing.MarketValue = existing.ClosingPrice.Value * existing.SharesOutstanding.Value;
        }
        else if (data.MarketValue.HasValue)
        {
            existing.MarketValue = data.MarketValue;
        }
        existing.BookValue = data.BookValue;
        existing.PbRatio = data.PbRatio;
        existing.Eps = data.Eps;
        existing.PeRatio = data.PeRatio;
        existing.Currency = data.Currency;
        existing.SlowDataFetchedAt = data.SlowDataFetchedAt;
    }
}
