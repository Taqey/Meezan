using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Common.Interfaces;

public interface IShariahSourceOpinionRepository
{
    Task<List<ShariahSourceOpinion>> GetByStockIdAsync(int stockId, CancellationToken cancellationToken = default);
    Task<ShariahSourceOpinion?> GetByStockIdAndSourceKeyAsync(int stockId, ShariahSourceKey sourceKey, CancellationToken cancellationToken = default);
    Task AddAsync(ShariahSourceOpinion opinion, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ShariahSourceOpinion> opinions, CancellationToken cancellationToken = default);
    void Update(ShariahSourceOpinion opinion);
    void RemoveRange(IEnumerable<ShariahSourceOpinion> opinions);
}
