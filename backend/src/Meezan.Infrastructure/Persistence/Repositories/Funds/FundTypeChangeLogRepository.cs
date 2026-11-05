using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundTypeChangeLogRepository : IFundTypeChangeLogRepository
{
    private readonly ApplicationDbContext _context;

    public FundTypeChangeLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FundTypeChangeLog log, CancellationToken cancellationToken = default)
    {
        await _context.FundTypeChangeLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(List<FundTypeChangeLog> Items, int TotalCount)> QueryPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _context.FundTypeChangeLogs
            .Include(l => l.Fund)
            .Include(l => l.Definition)
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking();

        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
