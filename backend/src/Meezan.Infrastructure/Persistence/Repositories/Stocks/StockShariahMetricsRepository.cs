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

namespace Meezan.Infrastructure.Persistence.Repositories.Stocks;

public class StockShariahMetricsRepository : IStockShariahMetricsRepository
{
    private readonly ApplicationDbContext _context;

    public StockShariahMetricsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockShariahMetrics?> GetByStockIdAsync(int stockId, CancellationToken cancellationToken = default)
    {
        return await _context.StockShariahMetrics
            .FirstOrDefaultAsync(m => m.StockId == stockId, cancellationToken);
    }

    public async Task AddAsync(StockShariahMetrics metrics, CancellationToken cancellationToken = default)
    {
        await _context.StockShariahMetrics.AddAsync(metrics, cancellationToken);
    }

    public void Update(StockShariahMetrics metrics)
    {
        _context.StockShariahMetrics.Update(metrics);
    }
}
