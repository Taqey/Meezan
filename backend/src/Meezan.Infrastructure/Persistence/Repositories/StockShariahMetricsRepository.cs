using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

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
