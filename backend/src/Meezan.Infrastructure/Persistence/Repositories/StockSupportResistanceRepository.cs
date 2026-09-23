using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class StockSupportResistanceRepository : IStockSupportResistanceRepository
{
    private readonly ApplicationDbContext _context;

    public StockSupportResistanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockSupportResistance?> GetByStockIdAsync(int stockId, CancellationToken ct = default)
    {
        return await _context.StockSupportResistance
            .FirstOrDefaultAsync(sr => sr.StockId == stockId, ct);
    }

    public async Task UpsertAsync(StockSupportResistance sr, CancellationToken ct = default)
    {
        var existing = await _context.StockSupportResistance
            .FirstOrDefaultAsync(e => e.StockId == sr.StockId, ct);

        if (existing is null)
        {
            await _context.StockSupportResistance.AddAsync(sr, ct);
        }
        else
        {
            existing.LastPrice = sr.LastPrice;
            existing.ChangePct = sr.ChangePct;
            existing.Pivot = sr.Pivot;
            existing.R1 = sr.R1;
            existing.R2 = sr.R2;
            existing.S1 = sr.S1;
            existing.S2 = sr.S2;
            existing.FetchedAt = sr.FetchedAt;
            _context.StockSupportResistance.Update(existing);
        }

        await _context.SaveChangesAsync(ct);
    }
}
