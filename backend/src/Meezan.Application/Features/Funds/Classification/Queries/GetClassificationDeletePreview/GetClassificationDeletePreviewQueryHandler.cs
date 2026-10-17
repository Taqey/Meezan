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

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationDeletePreview;

public sealed class GetClassificationDeletePreviewQueryHandler : IRequestHandler<GetClassificationDeletePreviewQuery, Result<ClassificationDeletePreviewDto>>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundClassificationService _classification;

    public GetClassificationDeletePreviewQueryHandler(
        IFundClassificationNodeRepository nodeRepo,
        IFundRepository fundRepo,
        IFundClassificationService classification)
    {
        _nodeRepo = nodeRepo;
        _fundRepo = fundRepo;
        _classification = classification;
    }

    public async Task<Result<ClassificationDeletePreviewDto>> Handle(GetClassificationDeletePreviewQuery request, CancellationToken cancellationToken)
    {
        var node = await _nodeRepo.GetByIdAsync(request.Id, includeDeleted: true, cancellationToken);
        if (node == null) return Error.NotFound("NodeNotFound", "");

        var desc = await _classification.GetDescendantIdsAsync(request.Id, cancellationToken);
        var funds = (await _fundRepo.GetAllAsync(cancellationToken)).Where(f => f.IsActive).ToList();
        var ctx = await _classification.LoadContextAsync(cancellationToken);
        var eff = await _classification.ResolveEffectiveManyAsync(funds, ctx);

        var inSubtree = eff.Count(kv => kv.Value.NodeId == request.Id || (kv.Value.NodeId.HasValue && desc.Contains(kv.Value.NodeId.Value)));
        var direct = eff.Count(kv => kv.Value.NodeId == request.Id);

        var allNodes = await _nodeRepo.GetAllAsync(includeDeleted: true, cancellationToken);
        var childCount = allNodes.Count(n => n.ParentId == request.Id && !n.IsDeleted);

        return new ClassificationDeletePreviewDto
        {
            Node = new AdminNodeBriefDto
            {
                Id = node.Id,
                Slug = node.Slug,
                NameAr = node.NameAr,
                NameEn = node.NameEn
            },
            DirectFunds = direct,
            DescendantFunds = inSubtree,
            ChildNodes = childCount
        };
    }
}
