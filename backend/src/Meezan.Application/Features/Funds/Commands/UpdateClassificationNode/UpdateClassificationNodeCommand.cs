using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Commands.UpdateClassificationNode;

public sealed record UpdateClassificationNodeCommand : IRequest<AdminClassificationNodeDto>, ITransactionalCommand
{
    public int Id { get; init; }
    public string? Slug { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int? DisplayOrder { get; init; }
    public bool? IsVisible { get; init; }
    public bool? RequiresChildSelection { get; init; }
    public string? ChildrenLabelAr { get; init; }
    public string? ChildrenLabelEn { get; init; }
    public string? AutoRuleJson { get; init; }
    public bool? AutoClassificationEnabled { get; init; }
}
