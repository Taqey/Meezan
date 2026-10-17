using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Features.RemovalCandidates.Queries.GetRemovalCandidates;

public sealed class GetRemovalCandidatesQueryHandler : IRequestHandler<GetRemovalCandidatesQuery, List<RemovalCandidateDto>>
{
    private readonly IRemovalCandidatesReadQuery _readQuery;

    public GetRemovalCandidatesQueryHandler(IRemovalCandidatesReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<List<RemovalCandidateDto>> Handle(GetRemovalCandidatesQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.GetRemovalCandidatesAsync(cancellationToken);
    }
}
