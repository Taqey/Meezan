using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundClassificationNodeRepository
{
    Task<FundClassificationNode?> GetByIdAsync(int id, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<FundClassificationNode?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<List<FundClassificationNode>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<List<FundClassificationNode>> GetChildrenAsync(int parentId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task AddAsync(FundClassificationNode node, CancellationToken cancellationToken = default);
    void Update(FundClassificationNode node);
    void Delete(FundClassificationNode node);
}
