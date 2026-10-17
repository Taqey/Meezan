using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;
using Meezan.Application.Features.Shariah.Commands.RecomputeVerdicts;
using Meezan.Application.Features.Shariah.Commands.RefreshShariahData;
using Meezan.Application.Features.Shariah.Commands.SeedShariahMap;
using Meezan.Application.Features.Shariah.Commands.UploadSourceReportFile;
using Meezan.Application.Features.Shariah.DTOs;
using Meezan.Application.Features.Shariah.Queries.GetSourcePdf;
using Meezan.Application.Features.Shariah.Queries.GetSourcePdfsStatus;
using Meezan.WebApi.Common;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Public;

[ApiController]
[Route("api/shariah")]
public class ShariahController : ControllerBase
{
    private readonly ISender _sender;

    public ShariahController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Seeds ShariahCompliance from shariah_map.json (bundled resource or uploaded payload).
    /// Creates any missing Stock rows by SymbolCode.
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(SeedShariahMapResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Seed([FromBody] Dictionary<string, ShariahSeedItemDto>? payload, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SeedShariahMapCommand(payload), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Manually triggers a refresh of external shariah data (from stocks_merged.json).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshShariahDataResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RefreshShariahDataCommand(TriggeredBy: "Manual"), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Recomputes and stores the Shariah verdict for ALL stocks with the shared
    /// verdict service. Idempotent — safe to re-run (used as the one-time backfill
    /// after deployment and as the admin "إعادة حساب الأحكام الشرعية" button).
    /// </summary>
    [HttpPost("recompute-verdicts")]
    public async Task<IActionResult> RecomputeVerdicts(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RecomputeVerdictsCommand(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Imports the Faisal Bank and Osoul boards' opinions from shariah_opinions_faisal_osoul.json.
    /// Each run fully replaces those two SourceKeys' opinion set (the other 5 boards are
    /// untouched); tickers the file no longer covers resolve to "لا يوجد رأي".
    /// Optional ?path= points at a specific file.
    /// </summary>
    [HttpPost("import-faisal-osoul")]
    [ProducesResponseType(typeof(ImportFaisalOsoulOpinionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImportFaisalOsoulOpinionsResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportFaisalOsoul([FromQuery] string? path, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ImportFaisalOsoulOpinionsCommand(path), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Per-source stored-PDF status for the admin "Currently stored files" box.
    /// Decides "exists" from BOTH the database record (opinion PdfUrl) AND the
    /// file on disk (URL-decoded path resolution): "exists" when the file is on
    /// disk, "missing-file" when the DB references a path absent from disk,
    /// "not-found" when neither exists. Never 404s itself.
    /// </summary>
    [HttpGet("source-pdfs/status")]
    [ProducesResponseType(typeof(List<SourcePdfStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSourcePdfsStatus(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSourcePdfsStatusQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Serves the stored source PDF for Faisal Bank or Ostoul.
    /// Resolves via the DB-recorded pdfUrl first (URL-decoded), then the
    /// canonical name, then a folder scan — same resolution as the status check.
    /// </summary>
    [HttpGet("source-pdf/{sourceKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSourcePdf(string sourceKey, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSourcePdfQuery(sourceKey), cancellationToken);
        if (result.IsFailure) return result.ToActionResult();

        // No download file name → inline disposition so the PDF opens in the
        // browser tab instead of forcing a download.
        return File(result.Value.FileStream, result.Value.ContentType);
    }

    /// <summary>
    /// Uploads a new PDF report for FaisalBank or Ostoul, replacing the existing file for that source.
    /// </summary>
    [HttpPost("sources/{sourceKey}/report-file")]
    [ProducesResponseType(typeof(UploadSourceReportFileResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(50_000_000)] // 50 MB max
    public async Task<IActionResult> UploadSourceReportFile(
        string sourceKey,
        IFormFile file,
        [FromForm] string? reportDate,
        CancellationToken cancellationToken)
    {
        var stream = file?.OpenReadStream() ?? Stream.Null;
        var command = new UploadSourceReportFileCommand(
            sourceKey,
            stream,
            file?.FileName ?? string.Empty,
            file?.ContentType ?? string.Empty,
            file?.Length ?? 0,
            reportDate);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}
