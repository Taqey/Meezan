using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Indices.Commands.UploadIndexFile;
using Meezan.Application.Features.Indices.DTOs;
using Meezan.Application.Features.Indices.Queries.GetIndexConstituents;
using Meezan.Application.Features.Indices.Queries.GetIndicesList;
using Meezan.WebApi.Common;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Public;

[ApiController]
[Route("api/indices")]
public class IndicesController : ControllerBase
{
    private readonly ISender _sender;

    public IndicesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns all indices with summary metadata and constituent counts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<IndexSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetIndicesListQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Returns paginated constituents for a specific index with market data, fair value, and weights.
    /// </summary>
    [HttpGet("{indexCode}/constituents")]
    [ProducesResponseType(typeof(IndexConstituentsPagedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConstituents(
        [FromRoute] string indexCode,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] string? search,
        [FromQuery] string? shariahStatus,
        [FromQuery] string[]? shariahStatuses,
        [FromQuery] string? priceComparison,
        [FromQuery] int? minCompliantSources,
        [FromQuery] decimal? minPeRatio,
        [FromQuery] decimal? maxPeRatio,
        [FromQuery] decimal? minPbRatio,
        [FromQuery] decimal? maxPbRatio,
        CancellationToken cancellationToken)
    {
        var query = new GetIndexConstituentsQuery(
            indexCode, page, pageSize, sortBy, sortDir, search, shariahStatus, shariahStatuses, priceComparison,
            minCompliantSources, minPeRatio, maxPeRatio, minPbRatio, maxPbRatio);
        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Upload an Excel file to update index constituents and stocks.
    /// </summary>
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
            return Result.Failure(Error.Validation("EmptyFile", "File must not be empty.", new { message = "File must not be empty." })).ToActionResult();
        }

        using var stream = file.OpenReadStream();
        var command = new UploadIndexFileCommand(
            IndexCode: indexCode,
            FileStream: stream,
            FileName: file.FileName,
            UploadedBy: User?.Identity?.Name ?? "Anonymous"
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}
