using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Stocks.DTOs;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Stocks.Commands.CreateStock;

public sealed class CreateStockCommandHandler : IRequestHandler<CreateStockCommand, StockManagementItemDto>
{
    private readonly IStockRepository _stockRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStockCommandHandler(
        IStockRepository stockRepository,
        ISectorRepository sectorRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _sectorRepository = sectorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StockManagementItemDto> Handle(CreateStockCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Ticker))
            throw new BadRequestException("Ticker is required.");

        var ticker = request.Ticker.Trim().ToUpperInvariant();
        var existing = await _stockRepository.GetByTickerIncludingInactiveAsync(ticker, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"A stock with ticker '{ticker}' already exists (Id={existing.Id}).");

        Sector? sector = null;
        if (request.SectorId.HasValue)
        {
            sector = await _sectorRepository.GetByIdAsync(request.SectorId.Value, cancellationToken);
            if (sector is null)
                throw new BadRequestException($"Sector with Id {request.SectorId.Value} does not exist.");
        }

        var stock = new Stock
        {
            Ticker = ticker,
            NameAr = string.IsNullOrWhiteSpace(request.NameAr) ? null : request.NameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? null : request.NameEn.Trim(),
            SectorId = request.SectorId,
            IsManual = request.SectorId.HasValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _stockRepository.AddAsync(stock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StockManagementItemDto(
            stock.Id, stock.Ticker, stock.NameAr, stock.NameEn,
            stock.SectorId, sector?.NameAr, sector?.NameEn,
            stock.IsManual, stock.IsActive);
    }
}
