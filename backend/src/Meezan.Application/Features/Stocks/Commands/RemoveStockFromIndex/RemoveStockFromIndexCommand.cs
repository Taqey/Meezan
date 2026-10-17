using MediatR;

namespace Meezan.Application.Features.Stocks.Commands.RemoveStockFromIndex;

public sealed record RemoveStockFromIndexCommand(int StockId, int IndexId) : IRequest;
