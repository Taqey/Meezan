using Meezan.Application.Common.Interfaces;
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
