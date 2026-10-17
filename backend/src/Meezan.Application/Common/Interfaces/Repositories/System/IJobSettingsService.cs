using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.System;

/// <summary>
/// Persisted job enabled/disabled flags. The singleton implementation
/// caches the values in memory and writes through to the database on every toggle.
/// </summary>
public interface IJobSettingsService
{
    /// <summary>Returns true when the job is allowed to run on schedule. Defaults to true when not found.</summary>
    bool IsEnabled(string jobKey);

    /// <summary>Returns all known job settings.</summary>
    Task<List<JobSetting>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Persists the new value and updates the in-memory cache. Returns the updated row.</summary>
    Task<JobSetting> SetEnabledAsync(string jobKey, bool enabled, CancellationToken ct = default);
}
