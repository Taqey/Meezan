namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncRunDetails;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public class GetSyncRunDetailsQueryHandler : IRequestHandler<GetSyncRunDetailsQuery, Result<List<FundSyncLogDetail>>>
{
    private readonly IFundSyncLogDetailRepository _detailRepo;

    public GetSyncRunDetailsQueryHandler(IFundSyncLogDetailRepository detailRepo)
    {
        _detailRepo = detailRepo;
    }

    public async Task<Result<List<FundSyncLogDetail>>> Handle(GetSyncRunDetailsQuery request, CancellationToken cancellationToken)
    {
        return await _detailRepo.GetByLogIdAsync(request.LogId, cancellationToken);
    }
}
