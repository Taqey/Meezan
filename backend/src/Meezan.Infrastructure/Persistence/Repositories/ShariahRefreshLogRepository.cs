using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
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
