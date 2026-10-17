using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Commands.UpdateClassificationNode;

public sealed record UpdateClassificationNodeCommand : IRequest<Result<AdminClassificationNodeDto>>, ITransactionalCommand, IInvalidatesFundCatalog
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
