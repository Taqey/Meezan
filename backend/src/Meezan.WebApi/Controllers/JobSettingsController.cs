using Meezan.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/job-settings")]
public class JobSettingsController : ControllerBase
{
    private readonly IJobSettingsService _jobSettingsService;

    public JobSettingsController(IJobSettingsService jobSettingsService)
    {
        _jobSettingsService = jobSettingsService;
    }

    /// <summary>
    /// Returns dictionary of jobKey -> enabled (e.g. {"LiveDaily": true, "SlowQuarterly": true, "MarketSnapshots": true}).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var settings = await _jobSettingsService.GetAllAsync(ct);
        var dict = settings.ToDictionary(s => s.JobKey, s => s.Enabled);
        return Ok(dict);
    }

    /// <summary>
    /// Updates the enabled state of a job and persists it to the database.
    /// </summary>
    [HttpPut("{jobKey}")]
    [ProducesResponseType(typeof(JobSettingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSetting(
        [FromRoute] string jobKey,
        [FromBody] UpdateJobSettingRequest req,
        CancellationToken ct)
    {
        var updated = await _jobSettingsService.SetEnabledAsync(jobKey, req.Enabled, ct);
        return Ok(new JobSettingResponse(updated.JobKey, updated.Enabled));
    }
}

public record UpdateJobSettingRequest(bool Enabled);
public record JobSettingResponse(string JobKey, bool Enabled);
