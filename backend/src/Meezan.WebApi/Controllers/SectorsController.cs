using MediatR;
using Meezan.Application.Features.Sectors.Queries.GetSectorsList;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/sectors")]
public class SectorsController : ControllerBase
{
    private readonly ISender _sender;

    public SectorsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns all sectors that contain at least one stock, sorted by stock count descending.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SectorSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSectorsListQuery(), cancellationToken);
        return Ok(result);
    }
}
