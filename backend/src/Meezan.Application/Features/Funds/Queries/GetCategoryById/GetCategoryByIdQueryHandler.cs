namespace Meezan.Application.Features.Funds.Queries.GetCategoryById;

using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, FundCategory?>
{
    private readonly IFundCategoryRepository _categoryRepo;

    public GetCategoryByIdQueryHandler(IFundCategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public async Task<FundCategory?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        return await _categoryRepo.GetByIdAsync(request.Id, cancellationToken);
    }
}
