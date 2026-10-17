using Meezan.Application.Common.Results;
using MediatR;
using Meezan.Application.Features.Indices.DTOs;

namespace Meezan.Application.Features.Indices.Queries.GetIndexConstituents;

public record GetIndexConstituentsQuery(
    string IndexCode,
    int? Page,
    int? PageSize,
    string? SortBy,
    string? SortDir,
    string? Search,
    string? ShariahStatus,
    string[]? ShariahStatuses = null,
    string? PriceComparison = null,
    int? MinCompliantSources = null,
    decimal? MinPeRatio = null,
    decimal? MaxPeRatio = null,
    decimal? MinPbRatio = null,
    decimal? MaxPbRatio = null
) : IRequest<Result<IndexConstituentsPagedResultDto>>;
