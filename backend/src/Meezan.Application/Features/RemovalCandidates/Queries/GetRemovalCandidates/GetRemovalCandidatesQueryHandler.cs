using MediatR;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Features.RemovalCandidates.Queries.GetRemovalCandidates;

public sealed class GetRemovalCandidatesQueryHandler : IRequestHandler<GetRemovalCandidatesQuery, Result<List<RemovalCandidateDto>>>
{
    private readonly IRemovalCandidatesReadQuery _readQuery;

    public GetRemovalCandidatesQueryHandler(IRemovalCandidatesReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<List<RemovalCandidateDto>>> Handle(GetRemovalCandidatesQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.GetRemovalCandidatesAsync(cancellationToken);
    }
}
