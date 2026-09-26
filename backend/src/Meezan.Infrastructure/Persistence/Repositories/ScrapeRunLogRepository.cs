using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

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
