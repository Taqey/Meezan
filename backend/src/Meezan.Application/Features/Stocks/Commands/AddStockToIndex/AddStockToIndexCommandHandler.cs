using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Services.Text;
using Meezan.Application.Common.Services.Market;
using Meezan.Application.Common.Services.Shariah;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Features.Stocks.DTOs;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Stocks.Commands.AddStockToIndex;

public sealed class AddStockToIndexCommandHandler : IRequestHandler<AddStockToIndexCommand, ConstituentManagementItemDto>
{
    private readonly IStockRepository _stockRepository;
    private readonly IIndexRepository _indexRepository;
    private readonly IShariahComplianceRepository _complianceRepository;
    private readonly IStockShariahMetricsRepository _metricsRepository;
    private readonly IShariahSourceOpinionRepository _opinionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddStockToIndexCommandHandler(
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

    public async Task<ConstituentManagementItemDto> Handle(AddStockToIndexCommand request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByIdAsync(request.StockId, cancellationToken);
        if (stock is null)
            throw new NotFoundException("Stock not found.");

        var index = await _indexRepository.GetByIdAsync(request.IndexId, cancellationToken);
        if (index is null)
            throw new NotFoundException("Index not found.");

        var existing = await _indexRepository.GetConstituentAsync(request.StockId, request.IndexId, cancellationToken);
        if (existing is not null)
        {
            return new ConstituentManagementItemDto(
                existing.Id, request.StockId, stock.Ticker,
                stock.NameAr, request.IndexId, index.Code, existing.Weight, existing.EffectiveDate);
        }

        // Sector weights are dynamic only — never store weight for sectoral indices.
        bool isSectoral = string.Equals(index.Code, "SECTORAL-INDICES", StringComparison.OrdinalIgnoreCase);

        var constituent = new IndexConstituent
        {
            StockId = request.StockId,
            IndexId = request.IndexId,
            Weight = isSectoral ? null : request.Weight,
            EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow.Date
        };

        await _indexRepository.AddConstituentAsync(constituent, cancellationToken);

        // EGX33 membership is an overall-verdict input: recompute that stock only,
        // in the same operation. Other indices never change the signal.
        if (ShariahStatusResolver.IsShariahIndexCode(index.Code))
        {
            await RecomputeOverallAsync(request.StockId, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConstituentManagementItemDto(
            constituent.Id, request.StockId, stock.Ticker,
            stock.NameAr, request.IndexId, index.Code, constituent.Weight, constituent.EffectiveDate);
    }

    private async Task RecomputeOverallAsync(int stockId, CancellationToken cancellationToken)
    {
        try
        {
            var compliance = await _complianceRepository.GetByStockIdAsync(stockId, cancellationToken);
            var metrics = await _metricsRepository.GetByStockIdAsync(stockId, cancellationToken);
            bool member = true; // being added to Shariah index right now
            if (compliance == null)
            {
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
