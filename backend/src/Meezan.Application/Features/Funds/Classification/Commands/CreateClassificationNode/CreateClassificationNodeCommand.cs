using MediatR;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;

namespace Meezan.Application.Features.Funds.Classification.Commands.CreateClassificationNode;

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
