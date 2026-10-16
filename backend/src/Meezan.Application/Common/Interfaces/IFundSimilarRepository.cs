using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundSimilarRepository
{
    Task<List<FundSimilar>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<FundSimilar> similars, CancellationToken cancellationToken = default);
    void ClearByFundId(int fundId);
}
