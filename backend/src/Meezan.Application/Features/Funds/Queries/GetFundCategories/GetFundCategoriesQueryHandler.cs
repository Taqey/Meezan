using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundCategories;

public sealed class GetFundCategoriesQueryHandler : IRequestHandler<GetFundCategoriesQuery, List<FundCategoryDto>>
{
    private readonly IFundCategoryRepository _categoryRepo;

    public GetFundCategoriesQueryHandler(IFundCategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public async Task<List<FundCategoryDto>> Handle(GetFundCategoriesQuery request, CancellationToken cancellationToken)
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
