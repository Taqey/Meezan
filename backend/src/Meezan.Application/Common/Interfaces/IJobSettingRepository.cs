using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IJobSettingRepository
{
    Task<List<JobSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<JobSetting?> GetByJobKeyAsync(string jobKey, CancellationToken cancellationToken = default);
    Task AddAsync(JobSetting setting, CancellationToken cancellationToken = default);
    void Update(JobSetting setting);
}
