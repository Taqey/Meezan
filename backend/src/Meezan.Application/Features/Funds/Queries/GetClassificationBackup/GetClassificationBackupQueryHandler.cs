using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Queries.GetClassificationBackup;

public sealed class GetClassificationBackupQueryHandler : IRequestHandler<GetClassificationBackupQuery, List<FundClassificationBackup>>
{
    private readonly IFundClassificationBackupRepository _repo;

    public GetClassificationBackupQueryHandler(IFundClassificationBackupRepository repo)
    {
        _repo = repo;
    }

    public Task<List<FundClassificationBackup>> Handle(GetClassificationBackupQuery request, CancellationToken cancellationToken)
    {
        return _repo.GetAllAsync(cancellationToken);
    }
}
