using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Categories.Commands.CreateFundCategory;

public sealed record CreateFundCategoryCommand : IRequest<Result<FundCategory>>, ITransactionalCommand
{
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public bool IsAutoDerived { get; init; }
    public bool IsIndexTracking { get; init; }
    public string? AutoRuleJson { get; init; }
    public bool IsVisible { get; init; } = true;
    public int DisplayOrder { get; init; }
}
