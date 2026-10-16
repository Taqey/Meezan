namespace Meezan.Application.Features.Funds.Queries.GetAdminFunds;

using MediatR;
using Meezan.Application.Common.Models;
using Meezan.Domain.Entities;

public record GetAdminFundsQuery(
    string? Search,
    string? Type,
    int Page,
    int PageSize) : IRequest<AdminFundsResultDto>;

public record AdminFundsResultDto(
    List<Fund> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
