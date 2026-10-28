using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Commands.AssignSector;

public sealed class AssignSectorCommandHandler : IRequestHandler<AssignSectorCommand, Result<StockManagementItemDto>>
{
    private readonly IStockRepository _stockRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIndexStaticCache _indexCache;

    public AssignSectorCommandHandler(
        IStockRepository stockRepository,
        ISectorRepository sectorRepository,
        IUnitOfWork unitOfWork,
        IIndexStaticCache indexCache)
    {
        _stockRepository = stockRepository;
        _sectorRepository = sectorRepository;
        _unitOfWork = unitOfWork;
        _indexCache = indexCache;
    }

    public async Task<Result<StockManagementItemDto>> Handle(AssignSectorCommand request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByIdWithSectorAsync(request.StockId, cancellationToken);
        if (stock is null)
        {
            return Error.NotFound("StockNotFound", $"Stock with Id {request.StockId} not found.");
        }

        var sector = await _sectorRepository.GetByIdAsync(request.SectorId, cancellationToken);
        if (sector is null)
        {
            return Error.Validation("SectorNotFound", $"Sector with Id {request.SectorId} does not exist.");
        }

        stock.SectorId = sector.Id;
        stock.Sector = sector;
        stock.IsManual = true;
        stock.UpdatedAt = DateTime.UtcNow;

        await _stockRepository.UpdateAsync(stock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate Sectoral-Indices cache so membership updates immediately
        await _indexCache.InvalidateAsync("SECTORAL-INDICES", cancellationToken);

        return new StockManagementItemDto(
            stock.Id, stock.Ticker, stock.NameAr, stock.NameEn,
            stock.SectorId, sector.NameAr, sector.NameEn,
            stock.IsManual, stock.IsActive);
    }
}
