using MediatR;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.GetSectorPickers;

public sealed record GetSectorPickersQuery : IRequest<List<SectorPickerItemDto>>;
