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

namespace Meezan.Infrastructure.Persistence.Repositories.Scraping;

public class ScrapeRunLogRepository : IScrapeRunLogRepository
{
    private readonly ApplicationDbContext _context;

    public ScrapeRunLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ScrapeRunLog> CreateAsync(ScrapeRunLog log, CancellationToken ct = default)
    {
        await _context.ScrapeRunLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
        return log;
    }

    public async Task UpdateAsync(ScrapeRunLog log, CancellationToken ct = default)
    {
        _context.ScrapeRunLogs.Update(log);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ScrapeRunLog?> GetLatestAsync(CancellationToken ct = default)
    {
        // Prefer the latest FINISHED run: an in-progress run still holds zeros
        // (TotalStocks/Succeeded/Failed are only written on finalisation), so
        // returning it would report "إجمالي الأسهم: 0" while a run is in flight.
        // Fall back to the latest row overall only when no finished run exists.
        return await _context.ScrapeRunLogs
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefaultAsync(l => l.FinishedAt != null, ct)
            ?? await _context.ScrapeRunLogs
                .OrderByDescending(l => l.StartedAt)
                .FirstOrDefaultAsync(ct);
    }
}
