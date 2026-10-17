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

namespace Meezan.Infrastructure.Persistence.Repositories.SystemSettings;

public class JobSettingRepository : IJobSettingRepository
{
    private readonly ApplicationDbContext _context;

    public JobSettingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobSettings
            .AsNoTracking()
            .OrderBy(s => s.JobKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<JobSetting?> GetByJobKeyAsync(string jobKey, CancellationToken cancellationToken = default)
    {
        return await _context.JobSettings
            .FirstOrDefaultAsync(s => s.JobKey == jobKey, cancellationToken);
    }

    public async Task AddAsync(JobSetting setting, CancellationToken cancellationToken = default)
    {
        await _context.JobSettings.AddAsync(setting, cancellationToken);
    }

    public void Update(JobSetting setting)
    {
        _context.JobSettings.Update(setting);
    }
}
