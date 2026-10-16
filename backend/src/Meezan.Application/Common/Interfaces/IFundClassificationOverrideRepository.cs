using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundClassificationOverrideRepository
{
    Task<List<FundClassificationOverride>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default);
    Task<List<FundClassificationOverride>> GetByNodeIdAsync(int nodeId, CancellationToken cancellationToken = default);
    Task<FundClassificationOverride?> GetAsync(int fundId, int nodeId, CancellationToken cancellationToken = default);
    Task<int> CountByNodeIdAsync(int nodeId, CancellationToken cancellationToken = default);
    Task AddAsync(FundClassificationOverride @override, CancellationToken cancellationToken = default);
    void Update(FundClassificationOverride @override);
    void Delete(FundClassificationOverride @override);
    void DeleteRange(IEnumerable<FundClassificationOverride> overrides);
}
