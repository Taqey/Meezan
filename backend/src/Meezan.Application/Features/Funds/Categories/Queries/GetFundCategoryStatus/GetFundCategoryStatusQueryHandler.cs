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
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategoryStatus;

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
