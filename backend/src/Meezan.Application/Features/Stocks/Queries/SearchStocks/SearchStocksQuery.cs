using MediatR;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.SearchStocks;

public sealed record SearchStocksQuery(string? Query) : IRequest<List<StockManagementItemDto>>;
