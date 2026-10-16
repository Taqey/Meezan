using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

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
