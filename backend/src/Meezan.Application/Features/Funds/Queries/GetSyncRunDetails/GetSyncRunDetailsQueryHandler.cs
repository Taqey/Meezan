namespace Meezan.Application.Features.Funds.Queries.GetSyncRunDetails;

using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

public class GetSyncRunDetailsQueryHandler : IRequestHandler<GetSyncRunDetailsQuery, List<FundSyncLogDetail>>
{
    private readonly IFundSyncLogDetailRepository _detailRepo;

    public GetSyncRunDetailsQueryHandler(IFundSyncLogDetailRepository detailRepo)
    {
        _detailRepo = detailRepo;
    }

    public async Task<List<FundSyncLogDetail>> Handle(GetSyncRunDetailsQuery request, CancellationToken cancellationToken)
    {
        return await _detailRepo.GetByLogIdAsync(request.LogId, cancellationToken);
    }
}
