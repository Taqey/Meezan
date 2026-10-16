using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundCategoryRepository
{
    Task<FundCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FundCategory?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<List<FundCategory>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<List<FundCategory>> GetVisibleAsync(CancellationToken cancellationToken = default);
    Task AddAsync(FundCategory category, CancellationToken cancellationToken = default);
    void Update(FundCategory category);
    void Delete(FundCategory category);
}
