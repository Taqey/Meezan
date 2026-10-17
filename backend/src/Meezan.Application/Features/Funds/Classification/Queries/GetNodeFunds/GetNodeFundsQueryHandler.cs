using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetNodeFunds;

public sealed class GetNodeFundsQueryHandler : IRequestHandler<GetNodeFundsQuery, Result<GetNodeFundsResultDto>>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundClassificationService _classification;

    public GetNodeFundsQueryHandler(
        IFundClassificationNodeRepository nodeRepo,
        IFundRepository fundRepo,
        IFundClassificationService classification)
    {
        _nodeRepo = nodeRepo;
        _fundRepo = fundRepo;
        _classification = classification;
    }

    public async Task<Result<GetNodeFundsResultDto>> Handle(GetNodeFundsQuery request, CancellationToken cancellationToken)
    {
        var node = await _nodeRepo.GetByIdAsync(request.Id, includeDeleted: true, cancellationToken);
        if (node == null) return Error.NotFound("NodeNotFound", "");

        var funds = (await _fundRepo.GetAllAsync(cancellationToken)).Where(f => f.IsActive).ToList();
        var ctx = await _classification.LoadContextAsync(cancellationToken);
        var eff = await _classification.ResolveEffectiveManyAsync(funds, ctx);
        var byId = funds.ToDictionary(f => f.Id);

        var q = (request.Search ?? string.Empty).Trim();
        var rows = new List<AdminNodeFundRowDto>();

        foreach (var (fid, e) in eff.Where(kv => kv.Value.NodeId == request.Id))
        {
            if (!byId.TryGetValue(fid, out var f)) continue;
            if (!string.IsNullOrWhiteSpace(q)
                && !f.Code.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !(f.NameAr ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)
                && !(f.NameEn ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(request.Source) && !string.Equals(e.Source, request.Source, StringComparison.OrdinalIgnoreCase))
            {
                var want = request.Source.Trim();
                bool match = (want == "يدوي" && e.Source == "Manual") || (want == "تلقائي" && e.Source == "Auto")
                    || (want == "SNDUK" && e.Source == "SNDUK");
                if (!match) continue;
            }

            rows.Add(new AdminNodeFundRowDto
            {
                Id = f.Id,
                Code = f.Code,
                NameAr = f.NameAr,
                NameEn = f.NameEn,
                AssetManagerName = f.AssetManagerName,
                Path = e.DisplayPathAr,
                Source = e.Source,
                Reason = e.Reason,
                NeedsReview = e.NeedsReview,
                IsManual = string.Equals(e.Source, "Manual", StringComparison.OrdinalIgnoreCase)
            });
        }

        rows = rows.OrderBy(r => r.Code).ToList();
        var total = rows.Count;
        var items = rows.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        var path = await _classification.GetPathAsync(request.Id, cancellationToken);

        return new GetNodeFundsResultDto
        {
            Node = new AdminNodeBriefDto
            {
                Id = node.Id,
                Slug = node.Slug,
                NameAr = node.NameAr,
                NameEn = node.NameEn
            },
            Path = path.Select(n => new AdminNodeBriefDto
            {
                Id = n.Id,
                Slug = n.Slug,
                NameAr = n.NameAr,
                NameEn = n.NameEn
            }).ToList(),
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)request.PageSize)
        };
    }
}
