using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Meezan.Application.Features.Market.Commands.RefreshMarketSnapshots;

public class RefreshMarketSnapshotsCommandHandler
    : IRequestHandler<RefreshMarketSnapshotsCommand, RefreshMarketSnapshotsResult>
{
    /// <summary>Politeness gap between page fetches (robots.txt asks crawl-delay 5).</summary>
    private static readonly TimeSpan PolitenessDelay = TimeSpan.FromSeconds(5);

    private readonly IIndexRepository _indexRepository;
    private readonly IMarketSnapshotRepository _snapshotRepository;
    private readonly IMubasherMarketSnapshotClient _snapshotClient;
    private readonly ILogger<RefreshMarketSnapshotsCommandHandler> _logger;

    public RefreshMarketSnapshotsCommandHandler(
        IIndexRepository indexRepository,
        IMarketSnapshotRepository snapshotRepository,
        IMubasherMarketSnapshotClient snapshotClient,
        ILogger<RefreshMarketSnapshotsCommandHandler> logger)
    {
        _indexRepository = indexRepository;
        _snapshotRepository = snapshotRepository;
        _snapshotClient = snapshotClient;
        _logger = logger;
    }

    public async Task<RefreshMarketSnapshotsResult> Handle(
        RefreshMarketSnapshotsCommand request, CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        var targets = new List<(string Kind, string Code, string MubasherCode, int? SectorId)>();

        // Indices: every DB index code that has a Mubasher slug.
        var indices = await _indexRepository.GetAllSummariesAsync(cancellationToken);
        foreach (var idx in indices)
        {
            var slug = MubasherMarketMap.IndexSlugs.TryGetValue(
                (idx.Code ?? string.Empty).Trim(), out var s) ? s : null;
            if (slug != null) targets.Add(("Index", idx.Code, slug, null));
        }

        // Sectors: every sector ID present in the map.
        // (Sector rows are resolved by ID at upsert time; unknown IDs skipped.)
        foreach (var (sectorId, slug) in MubasherMarketMap.SectorSlugs)
        {
            targets.Add(("Sector", sectorId.ToString(), slug, sectorId));
        }

        var succeeded = 0;
        var failed = 0;
        var first = true;
        foreach (var (kind, code, slug, _) in targets)
        {
            if (!first)
            {
                try { await Task.Delay(PolitenessDelay, cancellationToken); }
                catch (OperationCanceledException) { break; }
            }
            first = false;

            try
            {
                // One retry on transient failure; a failed target keeps its last good row.
                var quote = await _snapshotClient.ScrapeIndexSummaryAsync(slug, cancellationToken)
                    ?? await _snapshotClient.ScrapeIndexSummaryAsync(slug, cancellationToken);
                if (quote?.Value == null || quote?.ChangePct == null)
                {
                    failed++;
                    continue;
                }
                await _snapshotRepository.UpsertAsync(new MarketIndexSnapshot
                {
                    Kind = kind,
                    Code = code,
                    MubasherCode = slug,
                    Value = quote.Value,
                    Change = quote.Change,
                    ChangePct = quote.ChangePct,
                    SourceDateText = quote.SourceDateText,
                    Source = "Mubasher",
                    FetchedAt = DateTime.UtcNow
                }, cancellationToken);
                succeeded++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Market snapshot refresh failed for {Kind}/{Code}", kind, code);
                failed++;
            }
        }

        return new RefreshMarketSnapshotsResult(succeeded, failed, targets.Count, DateTime.UtcNow - started);
    }
}
