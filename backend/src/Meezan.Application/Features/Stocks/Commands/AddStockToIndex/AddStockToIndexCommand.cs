using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Commands.AddStockToIndex;

public sealed record AddStockToIndexCommand(
    int StockId,
    int IndexId,
    decimal? Weight,
    DateTime? EffectiveDate) : IRequest<Result<ConstituentManagementItemDto>>;
