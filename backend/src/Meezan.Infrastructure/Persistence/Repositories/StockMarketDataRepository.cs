using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

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
