namespace Meezan.Application.Features.Funds.Listing.Queries.GetAdminFunds;

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
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;

public class GetAdminFundsQueryHandler : IRequestHandler<GetAdminFundsQuery, Result<AdminFundsResultDto>>
{
    private readonly IFundRepository _fundRepo;

    public GetAdminFundsQueryHandler(IFundRepository fundRepo)
    {
        _fundRepo = fundRepo;
    }

    public async Task<Result<AdminFundsResultDto>> Handle(GetAdminFundsQuery request, CancellationToken cancellationToken)
    {
        var filter = new FundListFilter
        {
            Search = request.Search,
            Type = request.Type,
            Page = request.Page,
            PageSize = request.PageSize
        };
        var (items, totalCount) = await _fundRepo.QueryPagedAsync(filter, cancellationToken);
        return new AdminFundsResultDto(
            items,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }
}
