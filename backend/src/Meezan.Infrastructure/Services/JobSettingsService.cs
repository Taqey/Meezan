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
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Services;

/// <summary>
/// Singleton service that persists job enabled/disabled flags to the database
/// and caches them in memory. Workers call <see cref="IsEnabled"/> before every
/// scheduled run — no DB round-trip on the hot path.
/// </summary>
public class JobSettingsService : IJobSettingsService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, bool> _cache = new();
    private readonly Lock _lock = new();
    private bool _loaded = false;

    public JobSettingsService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool IsEnabled(string jobKey)
    {
        EnsureLoaded();
        lock (_lock)
        {
            return !_cache.TryGetValue(jobKey, out var val) || val;
        }
    }

    public async Task<List<JobSetting>> GetAllAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rows = await db.JobSettings.AsNoTracking().ToListAsync(ct);

        lock (_lock)
        {
            foreach (var row in rows)
                _cache[row.JobKey] = row.Enabled;
            _loaded = true;
        }

        return rows;
    }

    public async Task<JobSetting> SetEnabledAsync(string jobKey, bool enabled, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var row = await db.JobSettings.FirstOrDefaultAsync(j => j.JobKey == jobKey, ct);
        if (row == null)
        {
            row = new JobSetting { JobKey = jobKey, Enabled = enabled, UpdatedAt = DateTime.UtcNow };
            db.JobSettings.Add(row);
        }
        else
        {
            row.Enabled = enabled;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        lock (_lock)
        {
            _cache[jobKey] = enabled;
        }

        return row;
    }

    private void EnsureLoaded()
    {
        lock (_lock)
        {
            if (_loaded) return;
        }

        // Fire-and-forget initial load (best-effort; defaults to true if DB is unavailable).
        _ = Task.Run(async () =>
        {
            try { await GetAllAsync(); }
            catch { /* will retry on next access */ }
        });
    }
}
