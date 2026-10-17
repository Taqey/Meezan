using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Commands.RemoveFromSector;

public sealed record RemoveFromSectorCommand(int StockId) : IRequest<Result<StockManagementItemDto>>;
