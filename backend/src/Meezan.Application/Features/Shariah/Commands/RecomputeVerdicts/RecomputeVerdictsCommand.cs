using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Results;
using Meezan.Application.Common.Services.Shariah;
using Microsoft.Extensions.Logging;

namespace Meezan.Application.Features.Shariah.Commands.RecomputeVerdicts;

public record RecomputeVerdictsCommand : IRequest<Result<RecomputeVerdictsResult>>;

public record RecomputeVerdictsResult(
    bool Success,
    int TotalStocks,
    int RecomputedCount,
    int ChangedCount,
    int FailedCount,
    string? Message);

public class RecomputeVerdictsCommandHandler : IRequestHandler<RecomputeVerdictsCommand, Result<RecomputeVerdictsResult>>
{
    private readonly IStockRepository _stocks;
    private readonly IShariahComplianceRepository _compliance;
    private readonly IStockShariahMetricsRepository _metrics;
    private readonly IShariahSourceOpinionRepository _opinions;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RecomputeVerdictsCommandHandler> _logger;

    public RecomputeVerdictsCommandHandler(
        IStockRepository stocks,
        IShariahComplianceRepository compliance,
        IStockShariahMetricsRepository metrics,
        IShariahSourceOpinionRepository opinions,
        IUnitOfWork uow,
        ILogger<RecomputeVerdictsCommandHandler> logger)
    {
        _stocks = stocks;
        _compliance = compliance;
        _metrics = metrics;
        _opinions = opinions;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<RecomputeVerdictsResult>> Handle(RecomputeVerdictsCommand request, CancellationToken ct)
    {
        // Idempotent full recompute via the ONE shared verdict service: Halal ratio
        // verdict first, then the aggregated overall verdict (boards + EGX33 +
        // stored Halal). Safe to re-run: recomputes from current inputs and only
        // writes when the stored overall verdict actually differs.
        var all = await _stocks.GetAllAsync(ct);
        int recomputed = 0, changed = 0, failed = 0;
        var now = DateTime.UtcNow;

        foreach (var stock in all)
        {
            try
            {
                var m = await _metrics.GetByStockIdAsync(stock.Id, ct);
                var c = await _compliance.GetByStockIdAsync(stock.Id, ct);
                bool isNewRow = false;
                if (c == null)
                {
                    // Create the stored row so every stock has verdicts after deployment.
                    c = new Domain.Entities.ShariahCompliance
                    {
                        StockId = stock.Id,
                        Status = Domain.Enums.ShariahStatus.Pending,
                        Pct = null,
                        Note = null,
                        HasShariahBoard = false,
                        ViaScholarsOnly = false,
                        VerdictDetailsJson = null,
                        HalalStatus = null,
                        VerdictSource = null,
                        LastCheckedAt = now,
                        UpdatedAt = now
                    };
                    isNewRow = true;
                }

                var halal = ShariahVerdictService.ComputeForStock(m, c);
                ShariahVerdictService.TryApplyHalal(c, halal, _logger);

                var opinions = await _opinions.GetByStockIdAsync(stock.Id, ct);
                bool member = await _stocks.IsShariahIndexMemberAsync(stock.Id, ct);
                var beforeStatus = isNewRow ? (Domain.Enums.ShariahStatus?)null : c.Status;
                var beforeSource = c.VerdictSource;
                ShariahVerdictService.ApplyOverall(c, m?.EffectiveCoreActivityCompliant, opinions, member, _logger);
                bool verdictChanged = isNewRow || beforeStatus != c.Status || beforeSource != c.VerdictSource;

                if (isNewRow)
                    await _compliance.AddAsync(c, ct);
                else
                    _compliance.Update(c);
                recomputed++;
                if (verdictChanged) changed++;
            }
            catch (Exception ex)
            {
                // Safety: one stock never breaks the whole backfill.
                _logger.LogWarning(ex, "Verdict backfill failed for stock {Ticker}; keeping previous value.", stock.Ticker);
                failed++;
            }
        }

        await _uow.SaveChangesAsync(ct);
        return new RecomputeVerdictsResult(true, all.Count, recomputed, changed, failed,
            $"أُعيد حساب {recomputed} سهمًا وتغيّرت {changed} أحكامًا (فشل: {failed}).");
    }
}
