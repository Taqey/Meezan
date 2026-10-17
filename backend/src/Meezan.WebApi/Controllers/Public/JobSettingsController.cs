using MediatR;
using Meezan.Application.Features.JobSettings.Commands.UpdateJobSetting;
using Meezan.Application.Features.JobSettings.Queries.GetJobSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Public;

[ApiController]
[Route("api/job-settings")]
[Authorize]
public class JobSettingsController : ControllerBase
{
    private readonly ISender _sender;

    public JobSettingsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns dictionary of jobKey -> enabled (e.g. {"LiveDaily": true, "SlowQuarterly": true, "MarketSnapshots": true}).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetJobSettingsQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates the enabled state of a job and persists it to the database.
    /// </summary>
    [HttpPut("{jobKey}")]
    [ProducesResponseType(typeof(UpdateJobSettingResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSetting(
        [FromRoute] string jobKey,
        [FromBody] UpdateJobSettingRequest req,
        CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateJobSettingCommand(jobKey, req.Enabled), ct);
        return Ok(result);
    }
}
