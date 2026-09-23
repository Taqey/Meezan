using Meezan.Application.Features.Indices.Commands.UploadIndexFile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/indices")]
public class IndicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public IndicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns all indices with summary metadata and constituent counts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Meezan.Application.Features.Indices.Queries.GetIndicesList.IndexSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Meezan.Application.Features.Indices.Queries.GetIndicesList.GetIndicesListQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns paginated constituents for a specific index with market data, fair value, and weights.
    /// </summary>
    [HttpGet("{indexCode}/constituents")]
    [ProducesResponseType(typeof(Meezan.Application.Features.Indices.Queries.GetIndexConstituents.IndexConstituentsPagedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConstituents(
        [FromRoute] string indexCode,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] string? search,
        [FromQuery] string? shariahStatus,
        [FromQuery] string? priceComparison,
        CancellationToken cancellationToken)
    {
        var query = new Meezan.Application.Features.Indices.Queries.GetIndexConstituents.GetIndexConstituentsQuery(
            indexCode, page, pageSize, sortBy, sortDir, search, shariahStatus, priceComparison);
        var result = await _mediator.Send(query, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Upload an Excel file to update index constituents and stocks.
    /// </summary>
    /// <param name="indexCode">The code of the index (e.g. EGX30, EGX30TR, EGX70, EGX100, EGX35-LV, Shariah, Sectoral-Indices, TAMAYUZ)</param>
    /// <param name="file">The Excel (.xls or .xlsx) file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Counts of inserted, updated, and skipped items</returns>
    [HttpPost("{indexCode}/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadIndexFileResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadIndexFile(
        [FromRoute] string indexCode,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File must not be empty." });
        }

        using var stream = file.OpenReadStream();
        var command = new UploadIndexFileCommand(
            IndexCode: indexCode,
            FileStream: stream,
            FileName: file.FileName,
            UploadedBy: User?.Identity?.Name ?? "Anonymous"
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
