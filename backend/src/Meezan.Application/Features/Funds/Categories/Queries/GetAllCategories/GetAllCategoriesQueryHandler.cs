namespace Meezan.Application.Features.Funds.Categories.Queries.GetAllCategories;

using MediatR;
using Meezan.Application.Common.Results;
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
