using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationBackup;

public sealed class GetClassificationBackupQueryHandler : IRequestHandler<GetClassificationBackupQuery, Result<List<FundClassificationBackup>>>
{
    private readonly IFundClassificationBackupRepository _repo;

    public GetClassificationBackupQueryHandler(IFundClassificationBackupRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<List<FundClassificationBackup>>> Handle(GetClassificationBackupQuery request, CancellationToken cancellationToken)
    {
        return await _repo.GetAllAsync(cancellationToken);
    }
}
