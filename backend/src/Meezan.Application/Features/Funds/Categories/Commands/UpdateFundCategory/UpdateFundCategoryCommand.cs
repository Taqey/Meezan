using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Categories.Commands.UpdateFundCategory;

public sealed record UpdateFundCategoryCommand : IRequest<Result<FundCategory>>, ITransactionalCommand
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
