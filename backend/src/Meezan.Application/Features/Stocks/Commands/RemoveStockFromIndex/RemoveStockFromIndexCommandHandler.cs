using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Stocks.Commands.RemoveStockFromIndex;

public sealed class RemoveStockFromIndexCommandHandler : IRequestHandler<RemoveStockFromIndexCommand>
{
    private readonly IStockRepository _stockRepository;
    private readonly IIndexRepository _indexRepository;
    private readonly IShariahComplianceRepository _complianceRepository;
    private readonly IStockShariahMetricsRepository _metricsRepository;
    private readonly IShariahSourceOpinionRepository _opinionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveStockFromIndexCommandHandler(
        IStockRepository stockRepository,
        IIndexRepository indexRepository,
        IShariahComplianceRepository complianceRepository,
        IStockShariahMetricsRepository metricsRepository,
        IShariahSourceOpinionRepository opinionRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _indexRepository = indexRepository;
        _complianceRepository = complianceRepository;
        _metricsRepository = metricsRepository;
        _opinionRepository = opinionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveStockFromIndexCommand request, CancellationToken cancellationToken)
    {
        var constituent = await _indexRepository.GetConstituentWithIndexAsync(request.StockId, request.IndexId, cancellationToken);
        if (constituent is null)
            throw new NotFoundException($"Constituent for stock {request.StockId} and index {request.IndexId} not found.");

        var wasShariah = constituent.Index != null
            && ShariahStatusResolver.IsShariahIndexCode(constituent.Index.Code);

        _indexRepository.RemoveConstituent(constituent);

        if (wasShariah)
        {
            await RecomputeOverallAsync(request.StockId, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RecomputeOverallAsync(int stockId, CancellationToken cancellationToken)
    {
        try
        {
            var compliance = await _complianceRepository.GetByStockIdAsync(stockId, cancellationToken);
            var metrics = await _metricsRepository.GetByStockIdAsync(stockId, cancellationToken);
            bool member = false; // being removed from Shariah index right now
            if (compliance == null)
            {
                if (!member) return;
                var now = DateTime.UtcNow;
                compliance = new ShariahCompliance
                {
                    StockId = stockId,
                    Status = ShariahStatus.Pending,
                    HasShariahBoard = false,
                    LastCheckedAt = now,
                    UpdatedAt = now
                };
                if (metrics != null)
                {
                    var halal = ShariahVerdictService.ComputeForStock(metrics, compliance);
                    ShariahVerdictService.TryApplyHalal(compliance, halal);
                }
                await _complianceRepository.AddAsync(compliance, cancellationToken);
            }
            else
            {
                if (compliance.HalalStatus == null && metrics != null)
                {
                    var halal = ShariahVerdictService.ComputeForStock(metrics, compliance);
                    ShariahVerdictService.TryApplyHalal(compliance, halal);
                }
                _complianceRepository.Update(compliance);
            }

            var opinions = await _opinionRepository.GetByStockIdAsync(stockId, cancellationToken);
            ShariahVerdictService.ApplyOverall(
                compliance, metrics?.EffectiveCoreActivityCompliant, opinions, member);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Verdict] overall recompute failed for stock {stockId} ({ex.Message}); keeping previous stored value.");
        }
    }
}
