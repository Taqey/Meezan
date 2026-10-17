using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

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
