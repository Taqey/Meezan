using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Commands.RemoveFromSector;

public sealed class RemoveFromSectorCommandHandler : IRequestHandler<RemoveFromSectorCommand, Result<StockManagementItemDto>>
{
    private readonly IStockRepository _stockRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFromSectorCommandHandler(
        IStockRepository stockRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StockManagementItemDto>> Handle(RemoveFromSectorCommand request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByIdAsync(request.StockId, cancellationToken);
        if (stock is null)
        {
            return Error.NotFound("StockNotFound", $"Stock with Id {request.StockId} not found.");
        }

        stock.SectorId = null;
        stock.Sector = null;
        stock.IsManual = false;
        stock.UpdatedAt = DateTime.UtcNow;

        await _stockRepository.UpdateAsync(stock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StockManagementItemDto(
            stock.Id, stock.Ticker, stock.NameAr, stock.NameEn,
            null, null, null, stock.IsManual, stock.IsActive);
    }
}
