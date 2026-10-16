namespace Meezan.Application.Features.Funds.Queries.GetAllCategories;

using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, List<FundCategory>>
{
    private readonly IFundCategoryRepository _categoryRepo;

    public GetAllCategoriesQueryHandler(IFundCategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public async Task<List<FundCategory>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _categoryRepo.GetAllAsync(request.IncludeDeleted, cancellationToken);
    }
}
