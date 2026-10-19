using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundCategoryOverrideRepository
{
    Task<List<FundCategoryOverride>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default);
    Task<List<FundCategoryOverride>> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<List<FundCategoryOverride>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FundCategoryOverride?> GetAsync(int fundId, int categoryId, CancellationToken cancellationToken = default);
    Task AddAsync(FundCategoryOverride @override, CancellationToken cancellationToken = default);
    void Update(FundCategoryOverride @override);
    void Delete(FundCategoryOverride @override);
    Task ClearByFundIdAsync(int fundId, CancellationToken cancellationToken = default);
    Task ClearByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default);
}
