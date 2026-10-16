using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.CreateClassificationNode;

public sealed record CreateClassificationNodeCommand : IRequest<CreateClassificationNodeResultDto>, ITransactionalCommand
{
    public int? ParentId { get; init; }
    public string? Slug { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int? DisplayOrder { get; init; }
    public bool? IsVisible { get; init; }
    public bool? RequiresChildSelection { get; init; }
    public string? ChildrenLabelAr { get; init; }
    public string? ChildrenLabelEn { get; init; }
    public string? AutoRuleJson { get; init; }
    public bool? AutoClassificationEnabled { get; init; }
    public string? FundDisposition { get; init; }
}
