using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundCategoryChangeLogRepository : IFundCategoryChangeLogRepository
{
    private readonly ApplicationDbContext _context;

    public FundCategoryChangeLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FundCategoryChangeLog log, CancellationToken cancellationToken = default)
    {
        await _context.FundCategoryChangeLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(List<FundCategoryChangeLog> Items, int TotalCount)> QueryPagedAsync(FundCategoryChangeLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.FundCategoryChangeLogs
            .Include(l => l.Fund)
            .Include(l => l.Category)
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking();

        if (filter.FundId.HasValue)
            query = query.Where(l => l.FundId == filter.FundId.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(l => l.CategoryId == filter.CategoryId.Value);

        if (filter.Action.HasValue)
            query = query.Where(l => l.Action == filter.Action.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(l => l.CreatedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
