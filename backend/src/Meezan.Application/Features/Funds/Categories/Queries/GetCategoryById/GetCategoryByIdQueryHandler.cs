namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryById;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<FundCategory?>>
{
    private readonly IFundCategoryRepository _categoryRepo;

    public GetCategoryByIdQueryHandler(IFundCategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public async Task<Result<FundCategory?>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        return await _categoryRepo.GetByIdAsync(request.Id, cancellationToken);
    }
}
