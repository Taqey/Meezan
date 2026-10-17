using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundTypeOverrideRepository
{
    Task<FundTypeOverride?> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default);
    Task<List<FundTypeOverride>> GetByDefinitionIdAsync(int definitionId, CancellationToken cancellationToken = default);
    Task<List<FundTypeOverride>> GetByFundIdsAsync(IEnumerable<int> fundIds, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetAllFundOverridesAsync(CancellationToken cancellationToken = default);
    Task<int> CountByDefinitionIdAsync(int definitionId, CancellationToken cancellationToken = default);
    Task AddAsync(FundTypeOverride @override, CancellationToken cancellationToken = default);
    void Update(FundTypeOverride @override);
    void Delete(FundTypeOverride @override);
    void DeleteRange(IEnumerable<FundTypeOverride> overrides);
}
