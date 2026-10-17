using Meezan.Application.Common.Results;
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
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Commands.UpdateClassificationNode;

public sealed record UpdateClassificationNodeCommand : IRequest<Result<AdminClassificationNodeDto>>, ITransactionalCommand
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
