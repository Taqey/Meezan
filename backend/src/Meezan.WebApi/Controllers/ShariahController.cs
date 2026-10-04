using System.IO;
using System.Text.Json;
using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;
using Meezan.Application.Features.Shariah.Commands.RefreshShariahData;
using Meezan.Application.Features.Shariah.Commands.SeedShariahMap;
using Meezan.Application.Features.Shariah.DTOs;
using Meezan.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/shariah")]
public class ShariahController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly IShariahSourceOpinionRepository _opinions;
    private readonly IUnitOfWork _unitOfWork;

    public ShariahController(
        IMediator mediator,
        IWebHostEnvironment env,
        IShariahSourceOpinionRepository opinions,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _env = env;
        _opinions = opinions;
        _unitOfWork = unitOfWork;
    }

    // ── Stored source-PDF resolution (single source of truth) ──────────────
    // The database (ShariahSourceOpinion.PdfUrl per source) is the primary
    // reference; the file system is verified against it. No caller may rely on
    // hard-coded file names alone: names are only a fallback when the DB has
    // no path, plus a directory scan for that source.
    private const string PdfStorageSubPath = "uploads/shariah";

    /// <summary>DB status for one source's stored PDF report.</summary>
    public class SourcePdfStatusDto
    {
        public string SourceKey { get; set; } = string.Empty;
        public string? DbPdfUrl { get; set; }
        /// <summary>"exists" | "missing-file" | "not-found".</summary>
        public string Status { get; set; } = "not-found";
        public string? FileName { get; set; }
        public long? SizeBytes { get; set; }
        public DateTime? LastModifiedUtc { get; set; }
        public string? ViewPath { get; set; }
    }

    private static string? CanonicalSourceKey(string sourceKey) => sourceKey switch
    {
        "FaisalBank" => "FaisalBank",
        "Ostoul" or "Osoul" => "Ostoul",
        _ => null
    };

    private static ShariahSourceKey ToSourceKeyEnum(string canonical) => canonical switch
    {
        "FaisalBank" => ShariahSourceKey.FaisalBank,
        "Ostoul" => ShariahSourceKey.Osoul,
        _ => throw new ArgumentOutOfRangeException(nameof(canonical))
    };

    private static string CanonicalFileName(string canonical) => canonical switch
    {
        "FaisalBank" => "Faisal compliance-list.pdf",
        // NOTE: historical name carries a trailing space before the extension.
        "Ostoul" => "Ostoul compliance-list .pdf",
        _ => throw new ArgumentOutOfRangeException(nameof(canonical))
    };

    /// <summary>
    /// The one folder every PDF endpoint reads from and writes to. Always the
    /// app's wwwroot (explicitly suffixed — never the content root itself), so
    /// serve, upload and status can never drift apart.
    /// </summary>
    private string PdfStorageDir()
    {
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, PdfStorageSubPath);
    }

    private static string? FileNameFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        try
        {
            // Stored urls look like "/uploads/shariah/Ostoul compliance-list .pdf":
            // URL-decode first (%20 → space), then take the file name.
            var decoded = Uri.UnescapeDataString(url);
            return Path.GetFileName(decoded);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Locates the stored PDF for one source: (1) the DB-recorded pdfUrl
    /// (URL-decoded) mapped into the storage dir, (2) the canonical file name,
    /// (3) a case-insensitive scan of the storage dir for that source's files.
    /// Returns the full on-disk path, or null when nothing is on disk.
    /// </summary>
    private static string? FindStoredPdfFile(string storageDir, string canonical, string? dbPdfUrl)
    {
        var dbFileName = FileNameFromUrl(dbPdfUrl);
        if (!string.IsNullOrWhiteSpace(dbFileName))
        {
            var viaDb = Path.Combine(storageDir, dbFileName);
            if (System.IO.File.Exists(viaDb)) return viaDb;
        }

        var viaCanonical = Path.Combine(storageDir, CanonicalFileName(canonical));
        if (System.IO.File.Exists(viaCanonical)) return viaCanonical;

        // Fallback: scan the folder for this source's PDF (case-insensitive).
        try
        {
            if (!Directory.Exists(storageDir)) return null;
            var token = canonical == "FaisalBank" ? "faisal" : "osoul";
            var altToken = canonical == "FaisalBank" ? null : "ostoul";
            foreach (var file in Directory.GetFiles(storageDir, "*.pdf"))
            {
                var name = Path.GetFileName(file).ToLowerInvariant();
                if (name.Contains(token) || (altToken != null && name.Contains(altToken)))
                    return file;
            }
        }
        catch
        {
            // I/O errors mean "no verifiable file" — never throw from a probe.
        }

        return null;
    }

    private async Task<SourcePdfStatusDto> GetSourcePdfStatusAsync(
        string canonical, CancellationToken cancellationToken)
    {
        var enumKey = ToSourceKeyEnum(canonical);
        var rows = await _opinions.GetBySourceKeyAsync(enumKey, cancellationToken);
        var dbPdfUrl = rows
            .Select(r => r.PdfUrl)
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

        var storageDir = PdfStorageDir();
        var found = FindStoredPdfFile(storageDir, canonical, dbPdfUrl);

        var dto = new SourcePdfStatusDto
        {
            SourceKey = canonical,
            DbPdfUrl = dbPdfUrl,
            ViewPath = $"api/shariah/source-pdf/{canonical}"
        };

        if (found != null)
        {
            var info = new FileInfo(found);
            dto.Status = "exists";
            dto.FileName = info.Name;
            dto.SizeBytes = info.Length;
            dto.LastModifiedUtc = info.LastWriteTimeUtc;
        }
        else if (!string.IsNullOrWhiteSpace(dbPdfUrl))
        {
            // Referenced by opinion rows but absent from disk: distinct state,
            // not a plain "not found".
            dto.Status = "missing-file";
            dto.FileName = FileNameFromUrl(dbPdfUrl);
        }
        else
        {
            dto.Status = "not-found";
        }

        return dto;
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
        var result = new List<SourcePdfStatusDto>
        {
            await GetSourcePdfStatusAsync("FaisalBank", cancellationToken),
            await GetSourcePdfStatusAsync("Ostoul", cancellationToken)
        };
        return Ok(result);
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
        var canonical = CanonicalSourceKey(sourceKey);
        if (canonical == null) return NotFound();

        var enumKey = ToSourceKeyEnum(canonical);
        var rows = await _opinions.GetBySourceKeyAsync(enumKey, cancellationToken);
        var dbPdfUrl = rows
            .Select(r => r.PdfUrl)
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

        var filePath = FindStoredPdfFile(PdfStorageDir(), canonical, dbPdfUrl);
        if (filePath == null) return NotFound();

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, "application/pdf", Path.GetFileName(filePath));
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

        var canonical = CanonicalSourceKey(sourceKey)!;
        // Same folder and same naming convention the status check reads from,
        // so upload and status can never drift apart.
        var fileName = CanonicalFileName(canonical);

        var storageDir = PdfStorageDir();
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

        // Update PdfUrl on this source's opinion rows immediately so the stored-
        // files status (which reads the DB first) refreshes without waiting for
        // the next import-faisal-osoul run.
        var storedAt = $"/{PdfStorageSubPath}/{fileName}";
        var enumKey = ToSourceKeyEnum(canonical);
        var rows = await _opinions.GetBySourceKeyAsync(enumKey, cancellationToken);
        foreach (var row in rows)
        {
            row.PdfUrl = storedAt;
            _opinions.Update(row);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var reportDateParsed = DateTime.TryParse(reportDate, out var parsed) ? parsed : (DateTime?)null;

        return Ok(new
        {
            success = true,
            message = $"PDF uploaded and replaced for {canonical}.",
            sourceKey = canonical,
            fileName,
            storedAt,
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
