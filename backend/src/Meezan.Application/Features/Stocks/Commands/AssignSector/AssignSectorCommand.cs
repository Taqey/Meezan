using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Commands.AssignSector;

public sealed record AssignSectorCommand(int StockId, int SectorId) : IRequest<Result<StockManagementItemDto>>;
