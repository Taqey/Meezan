namespace Meezan.Application.Features.Funds.Queries.GetAdminFunds;

using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Common.Models;

public class GetAdminFundsQueryHandler : IRequestHandler<GetAdminFundsQuery, AdminFundsResultDto>
{
    private readonly IFundRepository _fundRepo;

    public GetAdminFundsQueryHandler(IFundRepository fundRepo)
    {
        _fundRepo = fundRepo;
    }

    public async Task<AdminFundsResultDto> Handle(GetAdminFundsQuery request, CancellationToken cancellationToken)
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
