using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundPriceHistoryRepository : IFundPriceHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public FundPriceHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundPriceHistory>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .Where(p => p.FundId == fundId)
            .OrderBy(p => p.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FundPriceHistory>> GetByFundIdAndDateRangeAsync(int fundId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .Where(p => p.FundId == fundId && p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<FundPriceHistory?> GetLatestAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .Where(p => p.FundId == fundId)
            .OrderByDescending(p => p.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FundPriceHistory?> GetByFundIdAndDateAsync(int fundId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .FirstOrDefaultAsync(p => p.FundId == fundId && p.Date.Date == date.Date, cancellationToken);
    }

    public async Task<DateTime?> GetMaxDateAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .Where(p => p.FundId == fundId)
            .MaxAsync(p => (DateTime?)p.Date, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<FundPriceHistory> history, CancellationToken cancellationToken = default)
    {
        await _context.FundPriceHistories.AddRangeAsync(history, cancellationToken);
    }

    public void UpdateRange(IEnumerable<FundPriceHistory> history)
    {
        _context.FundPriceHistories.UpdateRange(history);
    }
}
