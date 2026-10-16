using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Queries.GetFundCategoryStatus;

public sealed class GetFundCategoryStatusQueryHandler : IRequestHandler<GetFundCategoryStatusQuery, List<FundCategoryStatusDto>?>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryOverrideRepository _overrideRepo;

    public GetFundCategoryStatusQueryHandler(
        IFundRepository fundRepo,
        IFundCategoryRepository categoryRepo,
        IFundCategoryOverrideRepository overrideRepo)
    {
        _fundRepo = fundRepo;
        _categoryRepo = categoryRepo;
        _overrideRepo = overrideRepo;
    }

    public async Task<List<FundCategoryStatusDto>?> Handle(GetFundCategoryStatusQuery request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null) return null;

        var overrides = await _overrideRepo.GetByFundIdAsync(request.FundId, cancellationToken);
        var categories = await _categoryRepo.GetVisibleAsync(cancellationToken);

        return categories.Select(c =>
        {
            var overrideEntry = overrides.FirstOrDefault(o => o.CategoryId == c.Id);
            return new FundCategoryStatusDto(
                new FundCategoryInfoDto(c.Id, c.NameAr, c.NameEn, c.Slug, c.IsAutoDerived),
                overrideEntry?.Action == OverrideAction.Include || (c.IsAutoDerived && overrideEntry == null),
                overrideEntry != null ? (overrideEntry.Action == OverrideAction.Include ? "Manual" : "Excluded") : (c.IsAutoDerived ? "Auto" : "None"),
                overrideEntry?.Id
            );
        }).ToList();
    }
}
