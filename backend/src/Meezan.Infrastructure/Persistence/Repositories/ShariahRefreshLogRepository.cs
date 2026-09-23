using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class ShariahRefreshLogRepository : IShariahRefreshLogRepository
{
    private readonly ApplicationDbContext _context;

    public ShariahRefreshLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ShariahRefreshLog log, CancellationToken cancellationToken = default)
    {
        await _context.ShariahRefreshLogs.AddAsync(log, cancellationToken);
    }

    public async Task<List<ShariahRefreshLog>> GetRecentLogsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.ShariahRefreshLogs
            .OrderByDescending(l => l.RunAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
