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

public class StockMarketDataRepository : IStockMarketDataRepository
{
    private readonly ApplicationDbContext _context;

    public StockMarketDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockMarketData?> GetByStockIdAsync(int stockId, CancellationToken ct = default)
    {
        return await _context.StockMarketData
            .FirstOrDefaultAsync(m => m.StockId == stockId, ct);
    }

    public async Task UpsertAsync(StockMarketData data, CancellationToken ct = default)
    {
        var existing = await _context.StockMarketData
            .FirstOrDefaultAsync(m => m.StockId == data.StockId, ct);

        if (existing is null)
        {
            await _context.StockMarketData.AddAsync(data, ct);
        }
        else
        {
            existing.NominalValue = data.NominalValue;
            existing.MarketValue = data.MarketValue;
            existing.BookValue = data.BookValue;
            existing.PbRatio = data.PbRatio;
            existing.Eps = data.Eps;
            existing.PeRatio = data.PeRatio;
            existing.Currency = data.Currency;
            existing.High = data.High;
            existing.Low = data.Low;
            existing.Open = data.Open;
            existing.ClosingPrice = data.ClosingPrice;
            existing.SourceLastUpdateText = data.SourceLastUpdateText;
            existing.FetchedAt = data.FetchedAt;
            existing.SlowDataFetchedAt = data.SlowDataFetchedAt;
            _context.StockMarketData.Update(existing);
        }

        await _context.SaveChangesAsync(ct);
    }
}
