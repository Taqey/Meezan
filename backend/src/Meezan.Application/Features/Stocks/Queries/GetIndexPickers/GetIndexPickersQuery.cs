using MediatR;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.GetIndexPickers;

public sealed record GetIndexPickersQuery : IRequest<List<IndexPickerItemDto>>;
