namespace Meezan.Application.Features.Funds.Categories.Queries.GetAllCategories;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<FundCategory>>>
{
    private readonly IFundCategoryRepository _categoryRepo;

    public GetAllCategoriesQueryHandler(IFundCategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public async Task<Result<List<FundCategory>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _categoryRepo.GetAllAsync(request.IncludeDeleted, cancellationToken);
    }
}
