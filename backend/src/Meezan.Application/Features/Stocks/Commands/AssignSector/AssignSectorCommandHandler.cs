using MediatR;
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
