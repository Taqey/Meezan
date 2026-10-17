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
