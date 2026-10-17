namespace Meezan.Application.Features.Funds.Listing.Queries.GetAdminFunds;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetAdminFundsQuery(
    string? Search,
    string? Type,
    int Page,
    int PageSize) : IRequest<Result<AdminFundsResultDto>>;

public record AdminFundsResultDto(
    List<Fund> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
