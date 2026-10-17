using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.SearchStocks;

public sealed record SearchStocksQuery(string? Query) : IRequest<Result<List<StockManagementItemDto>>>;
