using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundClassificationChangeLogRepository : IFundClassificationChangeLogRepository
{
    private readonly ApplicationDbContext _context;

    public FundClassificationChangeLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FundClassificationChangeLog log, CancellationToken cancellationToken = default)
    {
        await _context.FundClassificationChangeLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(List<FundClassificationChangeLog> Items, int TotalCount)> QueryPagedAsync(int? fundId, int? nodeId, ClassificationChangeAction? action, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationChangeLogs
            .Include(l => l.Fund)
            .Include(l => l.Node)
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking();

        if (fundId.HasValue)
            query = query.Where(l => l.FundId == fundId.Value);

        if (nodeId.HasValue)
            query = query.Where(l => l.NodeId == nodeId.Value);

        if (action.HasValue)
            query = query.Where(l => l.Action == action.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
