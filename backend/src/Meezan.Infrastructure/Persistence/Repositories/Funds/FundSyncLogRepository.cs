using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundSyncLogRepository : IFundSyncLogRepository
{
    private readonly ApplicationDbContext _context;
    public FundSyncLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public FundSyncLog? LastAdded { get; private set; }

    public async Task<FundSyncLog?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundSyncLogs
            .OrderByDescending(l => l.RunAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(List<FundSyncLog> Items, int TotalCount)> QueryPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.FundSyncLogs
            .OrderByDescending(l => l.RunAt)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(FundSyncLog log, CancellationToken cancellationToken = default)
    {
        await _context.FundSyncLogs.AddAsync(log, cancellationToken);
        LastAdded = log;
    }

    public void Update(FundSyncLog log)
    {
        _context.FundSyncLogs.Update(log);
    }
}
