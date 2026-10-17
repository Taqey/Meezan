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
using Meezan.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class ShariahSourceOpinionRepository : IShariahSourceOpinionRepository
{
    private readonly ApplicationDbContext _context;

    public ShariahSourceOpinionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ShariahSourceOpinion>> GetByStockIdAsync(int stockId, CancellationToken cancellationToken = default)
    {
        return await _context.ShariahSourceOpinions
            .Where(o => o.StockId == stockId)
            .OrderBy(o => o.SourceKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShariahSourceOpinion?> GetByStockIdAndSourceKeyAsync(int stockId, ShariahSourceKey sourceKey, CancellationToken cancellationToken = default)
    {
        return await _context.ShariahSourceOpinions
            .FirstOrDefaultAsync(o => o.StockId == stockId && o.SourceKey == sourceKey, cancellationToken);
    }

    public async Task<List<ShariahSourceOpinion>> GetBySourceKeyAsync(ShariahSourceKey sourceKey, CancellationToken cancellationToken = default)
    {
        // Tracked + Stock included: the manual import updates/removes these rows in place
        // and matches them to the JSON by ticker.
        return await _context.ShariahSourceOpinions
            .Include(o => o.Stock)
            .Where(o => o.SourceKey == sourceKey)
            .OrderBy(o => o.Stock!.Ticker)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ShariahSourceOpinion opinion, CancellationToken cancellationToken = default)
    {
        await _context.ShariahSourceOpinions.AddAsync(opinion, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ShariahSourceOpinion> opinions, CancellationToken cancellationToken = default)
    {
        await _context.ShariahSourceOpinions.AddRangeAsync(opinions, cancellationToken);
    }

    public void Update(ShariahSourceOpinion opinion)
    {
        _context.ShariahSourceOpinions.Update(opinion);
    }

    public void RemoveRange(IEnumerable<ShariahSourceOpinion> opinions)
    {
        _context.ShariahSourceOpinions.RemoveRange(opinions);
    }
}
