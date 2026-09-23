using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IShariahRefreshLogRepository
{
    Task AddAsync(ShariahRefreshLog log, CancellationToken cancellationToken = default);
    Task<List<ShariahRefreshLog>> GetRecentLogsAsync(int count = 10, CancellationToken cancellationToken = default);
}
