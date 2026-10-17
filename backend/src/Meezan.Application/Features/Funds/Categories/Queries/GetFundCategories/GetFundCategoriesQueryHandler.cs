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

namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategories;

public sealed class GetFundCategoriesQueryHandler : IRequestHandler<GetFundCategoriesQuery, Result<List<FundCategoryDto>>>
{
    private readonly IFundCategoryRepository _categoryRepo;

    public GetFundCategoriesQueryHandler(IFundCategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public async Task<Result<List<FundCategoryDto>>> Handle(GetFundCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepo.GetVisibleAsync(cancellationToken);
        return categories.Select(c => new FundCategoryDto
        {
            Id = c.Id,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            Slug = c.Slug,
            Icon = c.Icon,
            Color = c.Color,
            IsAutoDerived = c.IsAutoDerived,
            DisplayOrder = c.DisplayOrder
        }).ToList();
    }
}
