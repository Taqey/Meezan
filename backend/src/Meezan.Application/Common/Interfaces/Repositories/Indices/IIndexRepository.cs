using Meezan.Domain.Entities;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Application.Common.Interfaces.Repositories.Indices;

public interface IIndexRepository
{
    Task<Index?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Index?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<Index>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default);
    Task UpdateAsync(Index index, CancellationToken cancellationToken = default);
    Task<IndexConstituent?> GetConstituentAsync(int stockId, int indexId, CancellationToken cancellationToken = default);
    Task<IndexConstituent?> GetConstituentWithIndexAsync(int stockId, int indexId, CancellationToken cancellationToken = default);
    Task AddConstituentAsync(IndexConstituent constituent, CancellationToken cancellationToken = default);
    void RemoveConstituent(IndexConstituent constituent);
    Task ReplaceConstituentsAsync(int indexId, IEnumerable<IndexConstituent> constituents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Current member stock IDs of one index. Used to diff membership before a
    /// replace so EGX33-driven overall verdicts recompute only for added/removed
    /// members, in the same upload operation.
    /// </summary>
    Task<HashSet<int>> GetConstituentStockIdsAsync(int indexId, CancellationToken cancellationToken = default);

    Task<List<IndexSummaryProjection>> GetAllSummariesAsync(CancellationToken cancellationToken = default);
}

