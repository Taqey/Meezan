using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.UpdateFundCategory;

public sealed record UpdateFundCategoryCommand : IRequest<FundCategory>, ITransactionalCommand
{
    public int Id { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? Slug { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public bool? IsIndexTracking { get; init; }
    public bool? IsVisible { get; init; }
    public int? DisplayOrder { get; init; }
    public string? AutoRuleJson { get; init; }
}
