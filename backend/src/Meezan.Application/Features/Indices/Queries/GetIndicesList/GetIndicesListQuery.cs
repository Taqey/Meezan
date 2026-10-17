using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Indices.DTOs;

namespace Meezan.Application.Features.Indices.Queries.GetIndicesList;

public record GetIndicesListQuery : IRequest<Result<List<IndexSummaryDto>>>;
