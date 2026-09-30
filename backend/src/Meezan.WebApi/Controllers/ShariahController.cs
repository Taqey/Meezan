using System.IO;
using System.Text.Json;
using MediatR;
using Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;
using Meezan.Application.Features.Shariah.Commands.RefreshShariahData;
using Meezan.Application.Features.Shariah.Commands.SeedShariahMap;
using Meezan.Application.Features.Shariah.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/shariah")]
public class ShariahController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public ShariahController(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
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
        Dictionary<string, ShariahSeedItemDto>? data = payload;

        // If no payload passed, read from embedded resource or local file
        if (data == null || data.Count == 0)
        {
            data = await LoadBundledShariahMapAsync(cancellationToken);
        }

        if (data == null || data.Count == 0)
        {
            return BadRequest(new { message = "No seed data found in request body or bundled shariah_map.json resource." });
        }

        var result = await _mediator.Send(new SeedShariahMapCommand(data), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Manually triggers a refresh of external shariah data (from stocks_merged.json).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshShariahDataResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshShariahDataCommand(TriggeredBy: "Manual"), cancellationToken);
        return Ok(result);
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
        var result = await _mediator.Send(new ImportFaisalOsoulOpinionsCommand(path), cancellationToken);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Serves the stored source PDF for Faisal Bank or Ostoul.
    /// </summary>
    [HttpGet("source-pdf/{sourceKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetSourcePdf(string sourceKey)
    {
        var fileName = sourceKey switch
        {
            "FaisalBank" => "Faisal compliance-list.pdf",
            "Ostoul" or "Osoul" => "Ostoul compliance-list .pdf",
            _ => null
        };

        if (fileName == null) return NotFound();

        var filePath = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads/shariah", fileName);
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, "application/pdf", fileName);
    }

    /// <summary>
    /// Uploads a new PDF report for FaisalBank or Ostoul, replacing the existing file for that source.
    /// </summary>
    [HttpPost("sources/{sourceKey}/report-file")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(50_000_000)] // 50 MB max
    public async Task<IActionResult> UploadSourceReportFile(
        string sourceKey,
        IFormFile file,
        [FromForm] string? reportDate,
        CancellationToken cancellationToken)
    {
        // Validate sourceKey
        if (sourceKey != "FaisalBank" && sourceKey != "Ostoul" && sourceKey != "Osoul")
        {
            return BadRequest(new { message = "sourceKey must be 'FaisalBank' or 'Ostoul'." });
        }

        // Validate file
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided." });
        }
        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "File must be a PDF." });
        }
        if (file.Length > 50_000_000)
        {
            return BadRequest(new { message = "File size exceeds 50 MB limit." });
        }

        var fileName = sourceKey switch
        {
            "FaisalBank" => "Faisal compliance-list.pdf",
            "Ostoul" or "Osoul" => "Ostoul compliance-list .pdf",
            _ => null // already validated
        };

        var storageDir = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads/shariah");
        Directory.CreateDirectory(storageDir);

        var filePath = Path.Combine(storageDir, fileName);

        // Delete old file if exists
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        // Save new file
        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // Update PdfUrl in ShariahSourceOpinions for this source (same path for all rows of this source)
        // We do this by calling the import command with the same JSON but it will re-persist the new PdfUrl.
        // Simpler: update directly via repository? But that's in Application layer.
        // For now, just return success; the next import-faisal-osoul run will pick up the new file.
        // To make it immediate, we'd need to update the DB rows here — that requires injecting the opinion repo.
        // Since this is an admin tool, the user will re-run the import after upload.
        // We'll return the new file reference and a note.

        var reportDateParsed = DateTime.TryParse(reportDate, out var parsed) ? parsed : (DateTime?)null;

        return Ok(new
        {
            success = true,
            message = $"PDF uploaded and replaced for {sourceKey}. Run import-faisal-osoul to refresh opinion records.",
            sourceKey,
            fileName,
            storedAt = $"/uploads/shariah/{fileName}",
            reportDate = reportDateParsed?.ToString("yyyy-MM-dd"),
            uploadedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }

    private async Task<Dictionary<string, ShariahSeedItemDto>?> LoadBundledShariahMapAsync(CancellationToken cancellationToken)
    {
        // 1. Try file path next to executing directory or app root
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "shariah_map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "shariah_map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "shariah_map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "shariah_map.json"),
            Path.Combine(_env.ContentRootPath, "shariah_map.json"),
            Path.Combine(_env.ContentRootPath, "..", "shariah_map.json"),
            Path.Combine(_env.ContentRootPath, "..", "..", "shariah_map.json")
        };

        foreach (var path in possiblePaths)
        {
            if (System.IO.File.Exists(path))
            {
                // Open with explicit UTF-8 (BOM-tolerant) so Arabic text is preserved.
                await using var fs = System.IO.File.OpenRead(path);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var json = await reader.ReadToEndAsync(cancellationToken);
                return JsonSerializer.Deserialize<Dictionary<string, ShariahSeedItemDto>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }

        // 2. Try embedded resource from Meezan.Infrastructure assembly
        var infraAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Meezan.Infrastructure");

        if (infraAssembly != null)
        {
            var resName = infraAssembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("shariah_map.json", StringComparison.OrdinalIgnoreCase));

            if (resName != null)
            {
                await using var stream = infraAssembly.GetManifestResourceStream(resName);
                if (stream != null)
                {
                    // Read with explicit UTF-8 so Arabic notes survive deserialization.
                    using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    var json = await reader.ReadToEndAsync(cancellationToken);
                    return JsonSerializer.Deserialize<Dictionary<string, ShariahSeedItemDto>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
        }

        return null;
    }
}
