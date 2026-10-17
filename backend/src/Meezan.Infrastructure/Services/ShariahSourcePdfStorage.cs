using System.Text.Json;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Features.Shariah.DTOs;
using Meezan.Domain.Enums;
using Microsoft.Extensions.Hosting;

namespace Meezan.Infrastructure.Services;

public class ShariahSourcePdfStorage : IShariahSourcePdfStorage
{
    private const string PdfStorageSubPath = "uploads/shariah";
    private readonly IHostEnvironment _env;
    private readonly IShariahSourceOpinionRepository _opinions;
    private readonly IUnitOfWork _unitOfWork;

    public ShariahSourcePdfStorage(
        IHostEnvironment env,
        IShariahSourceOpinionRepository opinions,
        IUnitOfWork unitOfWork)
    {
        _env = env;
        _opinions = opinions;
        _unitOfWork = unitOfWork;
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

    private string PdfStorageDir()
    {
        var webRoot = Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");
        return Path.Combine(webRoot, PdfStorageSubPath);
    }

    private static string? FileNameFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        try
        {
            var decoded = Uri.UnescapeDataString(url);
            return Path.GetFileName(decoded);
        }
        catch
        {
            return null;
        }
    }

    private static string? FindStoredPdfFile(string storageDir, string canonical, string? dbPdfUrl)
    {
        var dbFileName = FileNameFromUrl(dbPdfUrl);
        if (!string.IsNullOrWhiteSpace(dbFileName))
        {
            var viaDb = Path.Combine(storageDir, dbFileName);
            if (File.Exists(viaDb)) return viaDb;
        }

        var viaCanonical = Path.Combine(storageDir, CanonicalFileName(canonical));
        if (File.Exists(viaCanonical)) return viaCanonical;

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

        string status;
        string? fileName = null;
        long? sizeBytes = null;
        DateTime? lastModifiedUtc = null;

        if (found != null)
        {
            var info = new FileInfo(found);
            status = "exists";
            fileName = info.Name;
            sizeBytes = info.Length;
            lastModifiedUtc = info.LastWriteTimeUtc;
        }
        else if (!string.IsNullOrWhiteSpace(dbPdfUrl))
        {
            status = "missing-file";
            fileName = FileNameFromUrl(dbPdfUrl);
        }
        else
        {
            status = "not-found";
        }

        return new SourcePdfStatusDto(
            SourceKey: canonical,
            DbPdfUrl: dbPdfUrl,
            Status: status,
            FileName: fileName,
            SizeBytes: sizeBytes,
            LastModifiedUtc: lastModifiedUtc,
            ViewPath: $"api/shariah/source-pdf/{canonical}");
    }

    public async Task<List<SourcePdfStatusDto>> GetSourcePdfsStatusAsync(CancellationToken cancellationToken = default)
    {
        return new List<SourcePdfStatusDto>
        {
            await GetSourcePdfStatusAsync("FaisalBank", cancellationToken),
            await GetSourcePdfStatusAsync("Ostoul", cancellationToken)
        };
    }

    public async Task<SourcePdfFileResultDto?> GetSourcePdfStreamAsync(string sourceKey, CancellationToken cancellationToken = default)
    {
        var canonical = CanonicalSourceKey(sourceKey);
        if (canonical == null) return null;

        var enumKey = ToSourceKeyEnum(canonical);
        var rows = await _opinions.GetBySourceKeyAsync(enumKey, cancellationToken);
        var dbPdfUrl = rows
            .Select(r => r.PdfUrl)
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

        var filePath = FindStoredPdfFile(PdfStorageDir(), canonical, dbPdfUrl);
        if (filePath == null) return null;

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return new SourcePdfFileResultDto(stream, "application/pdf");
    }

    public async Task<UploadSourceReportFileResultDto> SaveSourceReportFileAsync(
        string sourceKey,
        Stream fileStream,
        string fileName,
        string contentType,
        long length,
        string? reportDate,
        CancellationToken cancellationToken = default)
    {
        if (sourceKey != "FaisalBank" && sourceKey != "Ostoul" && sourceKey != "Osoul")
        {
            throw new BadRequestException("sourceKey must be 'FaisalBank' or 'Ostoul'.");
        }

        if (fileStream == null || length == 0)
        {
            throw new BadRequestException("No file provided.");
        }
        if (!contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("File must be a PDF.");
        }
        if (length > 50_000_000)
        {
            throw new BadRequestException("File size exceeds 50 MB limit.");
        }

        var canonical = CanonicalSourceKey(sourceKey)!;
        var canonicalFileName = CanonicalFileName(canonical);

        var storageDir = PdfStorageDir();
        Directory.CreateDirectory(storageDir);

        var filePath = Path.Combine(storageDir, canonicalFileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(stream, cancellationToken);
        }

        var storedAt = $"/{PdfStorageSubPath}/{canonicalFileName}";
        var enumKey = ToSourceKeyEnum(canonical);
        var rows = await _opinions.GetBySourceKeyAsync(enumKey, cancellationToken);
        foreach (var row in rows)
        {
            row.PdfUrl = storedAt;
            _opinions.Update(row);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var reportDateParsed = DateTime.TryParse(reportDate, out var parsed) ? parsed : (DateTime?)null;

        return new UploadSourceReportFileResultDto(
            Success: true,
            Message: $"PDF uploaded and replaced for {canonical}.",
            SourceKey: canonical,
            FileName: canonicalFileName,
            StoredAt: storedAt,
            ReportDate: reportDateParsed?.ToString("yyyy-MM-dd"),
            UploadedAt: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}
