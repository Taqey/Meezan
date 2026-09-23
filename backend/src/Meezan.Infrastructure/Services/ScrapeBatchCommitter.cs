using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services;

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
        existing.MarketValue = data.MarketValue;
        existing.BookValue = data.BookValue;
        existing.PbRatio = data.PbRatio;
        existing.Eps = data.Eps;
        existing.PeRatio = data.PeRatio;
        existing.Currency = data.Currency;
        existing.SlowDataFetchedAt = data.SlowDataFetchedAt;
    }
}
