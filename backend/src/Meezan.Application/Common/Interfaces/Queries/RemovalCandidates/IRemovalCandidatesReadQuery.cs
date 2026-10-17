using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;

public interface IRemovalCandidatesReadQuery
{
    Task<List<RemovalCandidateDto>> GetRemovalCandidatesAsync(CancellationToken cancellationToken = default);
}
