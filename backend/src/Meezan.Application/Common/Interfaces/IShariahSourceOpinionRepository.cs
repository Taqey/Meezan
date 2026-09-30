using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Common.Interfaces;

public interface IShariahSourceOpinionRepository
{
    Task<List<ShariahSourceOpinion>> GetByStockIdAsync(int stockId, CancellationToken cancellationToken = default);
    Task<ShariahSourceOpinion?> GetByStockIdAndSourceKeyAsync(int stockId, ShariahSourceKey sourceKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every stored opinion of one board (Stock navigation included, for ticker matching).
    /// Used by the manual Faisal/Osoul import, which replaces that source's full opinion
    /// set per run — a stock with no row here means "لا يوجد رأي" for that board.
    /// </summary>
    Task<List<ShariahSourceOpinion>> GetBySourceKeyAsync(ShariahSourceKey sourceKey, CancellationToken cancellationToken = default);
    Task AddAsync(ShariahSourceOpinion opinion, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ShariahSourceOpinion> opinions, CancellationToken cancellationToken = default);
    void Update(ShariahSourceOpinion opinion);
    void RemoveRange(IEnumerable<ShariahSourceOpinion> opinions);
}
