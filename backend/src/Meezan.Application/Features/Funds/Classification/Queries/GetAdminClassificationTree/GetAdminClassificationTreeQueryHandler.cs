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

namespace Meezan.Application.Features.Funds.Classification.Queries.GetAdminClassificationTree;

public sealed class GetAdminClassificationTreeQueryHandler : IRequestHandler<GetAdminClassificationTreeQuery, Result<List<AdminClassificationNodeDto>>>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundClassificationService _classification;

    public GetAdminClassificationTreeQueryHandler(
        IFundClassificationNodeRepository nodeRepo,
        IFundRepository fundRepo,
        IFundClassificationService classification)
    {
        _nodeRepo = nodeRepo;
        _fundRepo = fundRepo;
        _classification = classification;
    }

    public async Task<Result<List<AdminClassificationNodeDto>>> Handle(GetAdminClassificationTreeQuery request, CancellationToken cancellationToken)
    {
        var nodes = await _nodeRepo.GetAllAsync(request.IncludeDeleted, cancellationToken);
        var funds = (await _fundRepo.GetAllAsync(cancellationToken)).Where(f => f.IsActive).ToList();

        var ctx = await _classification.LoadContextAsync(cancellationToken);
        var eff = await _classification.ResolveEffectiveManyAsync(funds, ctx);

        var direct = new Dictionary<int, int>();
        foreach (var e in eff.Values)
        {
            if (e.NodeId.HasValue) direct[e.NodeId.Value] = direct.GetValueOrDefault(e.NodeId.Value) + 1;
        }

        var kids = nodes.Where(n => n.ParentId.HasValue).GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(n => n.Id).ToList());
        var total = new Dictionary<int, int>(direct);
        int TotalOf(int id)
        {
            int t = direct.GetValueOrDefault(id);
            if (kids.TryGetValue(id, out var list))
                foreach (var k in list) t += TotalOf(k);
            total[id] = t;
            return t;
        }
        foreach (var n in nodes.Where(n => n.ParentId == null)) TotalOf(n.Id);

        return nodes.Select(n => new AdminClassificationNodeDto
        {
            Id = n.Id,
            ParentId = n.ParentId,
            Level = n.Level,
            Slug = n.Slug,
            NameAr = n.NameAr,
            NameEn = n.NameEn,
            Icon = n.Icon,
            Color = n.Color,
            DisplayOrder = n.DisplayOrder,
            IsVisible = n.IsVisible,
            IsDeleted = n.IsDeleted,
            DeletedAt = n.DeletedAt,
            DeletedReason = n.DeletedReason,
            RequiresChildSelection = n.RequiresChildSelection,
            ChildrenLabelAr = n.ChildrenLabelAr,
            ChildrenLabelEn = n.ChildrenLabelEn,
            AutoRuleJson = n.AutoRuleJson,
            AutoClassificationEnabled = n.AutoClassificationEnabled,
            CreatedAt = n.CreatedAt,
            UpdatedAt = n.UpdatedAt,
            CreatedBy = n.CreatedBy,
            UpdatedBy = n.UpdatedBy,
            DirectCount = direct.GetValueOrDefault(n.Id),
            TotalCount = total.GetValueOrDefault(n.Id)
        }).ToList();
    }
}
