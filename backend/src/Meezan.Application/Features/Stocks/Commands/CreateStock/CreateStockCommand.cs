using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Commands.CreateStock;

public sealed record CreateStockCommand(
    string Ticker,
    string? NameAr,
    string? NameEn,
    int? SectorId) : IRequest<Result<StockManagementItemDto>>;
