using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Commands.AssignSector;

public sealed class AssignSectorCommandHandler : IRequestHandler<AssignSectorCommand, StockManagementItemDto>
{
    private readonly IStockRepository _stockRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignSectorCommandHandler(
        IStockRepository stockRepository,
        ISectorRepository sectorRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _sectorRepository = sectorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StockManagementItemDto> Handle(AssignSectorCommand request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByIdWithSectorAsync(request.StockId, cancellationToken);
        if (stock is null)
            throw new NotFoundException($"Stock with Id {request.StockId} not found.");

        var sector = await _sectorRepository.GetByIdAsync(request.SectorId, cancellationToken);
        if (sector is null)
            throw new BadRequestException($"Sector with Id {request.SectorId} does not exist.");

        stock.SectorId = sector.Id;
        stock.Sector = sector;
        stock.IsManual = true;
        stock.UpdatedAt = DateTime.UtcNow;

        await _stockRepository.UpdateAsync(stock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StockManagementItemDto(
            stock.Id, stock.Ticker, stock.NameAr, stock.NameEn,
            stock.SectorId, sector.NameAr, sector.NameEn,
            stock.IsManual, stock.IsActive);
    }
}
