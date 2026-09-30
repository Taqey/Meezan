using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;

/// <summary>
/// Manual, re-runnable import of the two manually-published boards (Faisal Bank, Osoul)
/// from <c>shariah_opinions_faisal_osoul.json</c> — their published PDF reports are the
/// source of truth, the scraper no longer writes these two SourceKeys.
///
/// <para>
/// Universal three-state rule, per (stock, board) pair, for all 7 boards:
/// board explicitly lists the stock as compliant → <c>"compliant"</c>;
/// board explicitly lists it as non-compliant → <c>"non_compliant"</c>;
/// stock not covered by that board at all → no row at all (status null = "لا يوجد رأي").
/// A missing opinion is never defaulted to compliant and never to non-compliant.
/// </para>
///
/// <para>
/// Each run REPLACES that source's full opinion set (never additive): rows for tickers the
/// file no longer lists — including stale scraper values — are deleted, so those pairs
/// resolve to "no opinion". Tickers listed in a source's <c>has_shariah_board</c> get no
/// opinion row for that source: they follow the existing HasShariahBoard convention
/// (excluded from that source's opinion/ratio evaluation) and the persisted
/// ShariahCompliance.HasShariahBoard flag is not touched here.
/// </para>
/// </summary>
public record ImportFaisalOsoulOpinionsCommand(string? JsonPath = null)
    : IRequest<ImportFaisalOsoulOpinionsResult>;

public record ImportSourceOpinionsResult(
    string SourceKey,
    string? ReportDate,
    int InsertedCount,
    int UpdatedCount,
    int RemovedCount,
    int BoardListedSkippedCount,
    IReadOnlyList<string> UnknownTickers,
    IReadOnlyList<string> Conflicts);

public record ImportFaisalOsoulOpinionsResult(
    bool Success,
    string? Message,
    string FilePath,
    IReadOnlyList<ImportSourceOpinionsResult> Sources);

/// <summary>Root of shariah_opinions_faisal_osoul.json (the "_notes" block is ignored).</summary>
public class FaisalOsoulOpinionsFile
{
    public FaisalOsoulSourceBlock? FaisalBank { get; set; }
    [JsonPropertyName("ostoul")]
    public FaisalOsoulSourceBlock? Ostoul { get; set; }
}

public class FaisalOsoulSourceBlock
{
    [JsonPropertyName("report_date")]
    public string? ReportDate { get; set; }

    [JsonPropertyName("compliant")]
    public List<string> Compliant { get; set; } = new();

    [JsonPropertyName("non_compliant")]
    public List<string> NonCompliant { get; set; } = new();

    [JsonPropertyName("has_shariah_board")]
    public List<string> HasShariahBoard { get; set; } = new();
}

public class ImportFaisalOsoulOpinionsCommandHandler
    : IRequestHandler<ImportFaisalOsoulOpinionsCommand, ImportFaisalOsoulOpinionsResult>
{
    private const string FileName = "shariah_opinions_faisal_osoul.json";
    private const string FaisalPdfName = "Faisal compliance-list.pdf";
    private const string OsoulPdfName = "Ostoul compliance-list .pdf";
    private const string StorageSubPath = "uploads/shariah";

    private readonly IStockRepository _stockRepository;
    private readonly IShariahSourceOpinionRepository _opinionRepository;
    private readonly IShariahComplianceRepository _complianceRepository;
    private readonly IStockShariahMetricsRepository _metricsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportFaisalOsoulOpinionsCommandHandler(
        IStockRepository stockRepository,
        IShariahSourceOpinionRepository opinionRepository,
        IShariahComplianceRepository complianceRepository,
        IStockShariahMetricsRepository metricsRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _opinionRepository = opinionRepository;
        _complianceRepository = complianceRepository;
        _metricsRepository = metricsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportFaisalOsoulOpinionsResult> Handle(
        ImportFaisalOsoulOpinionsCommand request, CancellationToken cancellationToken)
    {
        var jsonPath = ResolvePath(request.JsonPath);
        if (jsonPath == null)
        {
            return new ImportFaisalOsoulOpinionsResult(
                false,
                $"Could not find {FileName}. Pass an explicit path or place the file in the working directory or any parent folder.",
                request.JsonPath ?? FileName,
                Array.Empty<ImportSourceOpinionsResult>());
        }

        // Resolve PDF paths (same directory as JSON)
        var jsonDir = Path.GetDirectoryName(jsonPath)!;
        var faisalPdfPath = Path.Combine(jsonDir, FaisalPdfName);
        var osoulPdfPath = Path.Combine(jsonDir, OsoulPdfName);

        // Copy PDFs to persistent storage (wwwroot/uploads/shariah) and get their relative URLs
        var faisalPdfUrl = await StorePdfAsync(faisalPdfPath, FaisalPdfName, cancellationToken);
        var osoulPdfUrl = await StorePdfAsync(osoulPdfPath, OsoulPdfName, cancellationToken);

        FaisalOsoulOpinionsFile? file;
        try
        {
            var json = await File.ReadAllTextAsync(jsonPath, Encoding.UTF8, cancellationToken);
            file = JsonSerializer.Deserialize<FaisalOsoulOpinionsFile>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return new ImportFaisalOsoulOpinionsResult(
                false, $"Failed to read {jsonPath}: {ex.Message}", jsonPath, Array.Empty<ImportSourceOpinionsResult>());
        }

        if (file == null || (file.FaisalBank == null && file.Ostoul == null))
        {
            return new ImportFaisalOsoulOpinionsResult(
                false, $"No FaisalBank/Ostoul block found in {jsonPath}.", jsonPath, Array.Empty<ImportSourceOpinionsResult>());
        }

        var stocks = await _stockRepository.GetAllAsync(cancellationToken);
        var byTicker = new Dictionary<string, Stock>(StringComparer.OrdinalIgnoreCase);
        foreach (var stock in stocks)
        {
            var ticker = stock.Ticker?.Trim();
            if (!string.IsNullOrEmpty(ticker)) byTicker[ticker] = stock;
        }

        var now = DateTime.UtcNow;
        var results = new List<ImportSourceOpinionsResult>();

        if (file.FaisalBank != null)
        {
            results.Add(await ImportBlockAsync(
                ShariahSourceKey.FaisalBank, "بنك فيصل الإسلامي", file.FaisalBank,
                byTicker, now, faisalPdfUrl, cancellationToken));
        }

        if (file.Ostoul != null)
        {
            results.Add(await ImportBlockAsync(
                ShariahSourceKey.Osoul, "أسطول", file.Ostoul,
                byTicker, now, osoulPdfUrl, cancellationToken));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Persist resolved compliance status ──────────────────────────────
        // After writing opinions, re-evaluate each affected stock's stored
        // ShariahCompliance.Status so readers that bypass EffectiveStatus
        // always see the right answer.
        await PersistResolvedStatusesAsync(byTicker, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var message = string.Join("; ", results.Select(r =>
            $"{r.SourceKey}: +{r.InsertedCount} / ~{r.UpdatedCount} / -{r.RemovedCount} " +
            $"(board-listed skipped {r.BoardListedSkippedCount}, unknown {r.UnknownTickers.Count}, conflicts {r.Conflicts.Count})"));

        return new ImportFaisalOsoulOpinionsResult(true, message, jsonPath, results);
    }

    /// <summary>
    /// For every stock touched by this import run, re-evaluate <see cref="ShariahCompliance.Status"/>
    /// using the same rule as <see cref="ShariahStatusResolver.EffectiveStatus"/>:
    /// activity gate wins; otherwise any board opinion of "compliant" → Compliant.
    /// Also ensure <see cref="ShariahCompliance.HasShariahBoard"/> is updated so stocks that
    /// receive standard board opinions do not retain a stale HasShariahBoard flag unless a board
    /// note actually specifies it.
    /// </summary>
    private async Task PersistResolvedStatusesAsync(
        Dictionary<string, Stock> byTicker,
        CancellationToken cancellationToken)
    {
        // Process ALL known stocks: we cannot predict which stocks were implicitly
        // affected by opinion removals (e.g. a ticker removed from a board's list).
        // Only stocks that have a ShariahCompliance row are updated.
        var now = DateTime.UtcNow;
        foreach (var stock in byTicker.Values)
        {
            var compliance = await _complianceRepository.GetByStockIdAsync(stock.Id, cancellationToken);
            if (compliance == null) continue;

            var metrics = await _metricsRepository.GetByStockIdAsync(stock.Id, cancellationToken);
            var opinions = await _opinionRepository.GetByStockIdAsync(stock.Id, cancellationToken);

            bool dirty = false;

            // Clear stale HasShariahBoard flag if stock receives standard opinions and has no board notes
            bool hasBoardNow = Common.ShariahBoardDetector.IsBoardNote(compliance.Note)
                || opinions.Any(o => Common.ShariahBoardDetector.IsBoardNote(o.Note));
            if (compliance.HasShariahBoard != hasBoardNow)
            {
                compliance.HasShariahBoard = hasBoardNow;
                dirty = true;
            }

            var resolved = ShariahStatusResolver.EffectiveStatus(compliance, metrics, opinions);
            if (resolved.HasValue && compliance.Status != resolved.Value)
            {
                compliance.Status = resolved.Value;
                dirty = true;
            }

            if (dirty)
            {
                compliance.UpdatedAt = now;
                _complianceRepository.Update(compliance);
            }
        }
    }

    private async Task<string?> StorePdfAsync(string sourcePath, string fileName, CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
        {
            return null; // PDF not provided — link will be hidden
        }

        var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var storageDir = Path.Combine(webRoot, StorageSubPath);
        Directory.CreateDirectory(storageDir);

        var destPath = Path.Combine(storageDir, fileName);
        File.Copy(sourcePath, destPath, true); // overwrite if exists

        // Return relative URL path for frontend
        return $"/{StorageSubPath}/{fileName}";
    }

    private async Task<ImportSourceOpinionsResult> ImportBlockAsync(
        ShariahSourceKey key,
        string sourceLabel,
        FaisalOsoulSourceBlock block,
        Dictionary<string, Stock> byTicker,
        DateTime now,
        string? pdfUrl,
        CancellationToken cancellationToken)
    {
        DateTime? reportDate = DateTime.TryParse(block.ReportDate, out var parsed) ? parsed : null;

        var compliant = Normalized(block.Compliant);
        var nonCompliant = Normalized(block.NonCompliant);
        var boardListed = Normalized(block.HasShariahBoard);

        var conflicts = compliant.Intersect(nonCompliant).OrderBy(t => t, StringComparer.Ordinal).ToList();
        compliant.ExceptWith(conflicts);
        nonCompliant.ExceptWith(conflicts);

        var verdicts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in compliant) verdicts[t] = "compliant";
        foreach (var t in nonCompliant) verdicts[t] = "non_compliant";

        foreach (var t in boardListed) verdicts.Remove(t);

        var note = $"استيراد يدوي من تقرير {sourceLabel}" +
                   (reportDate.HasValue ? $" المؤرخ {reportDate:yyyy-MM-dd}" : string.Empty) + ".";

        var existingRows = await _opinionRepository.GetBySourceKeyAsync(key, cancellationToken);
        var rowByTicker = new Dictionary<string, ShariahSourceOpinion>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in existingRows)
        {
            var ticker = row.Stock?.Ticker?.Trim();
            if (!string.IsNullOrEmpty(ticker) && !rowByTicker.ContainsKey(ticker))
            {
                rowByTicker[ticker] = row;
            }
        }

        int inserted = 0;
        int updated = 0;
        var unknown = new List<string>();

        foreach (var (ticker, status) in verdicts)
        {
            if (!byTicker.TryGetValue(ticker, out var stock))
            {
                unknown.Add(ticker);
                continue;
            }

            if (rowByTicker.TryGetValue(ticker, out var row))
            {
                row.Status = status;
                row.Percentage = null;
                row.Note = note;
                row.PdfUrl = pdfUrl;
                row.SourceLastUpdated = reportDate;
                row.FetchedAt = now;
                row.ExtraData = null;
                _opinionRepository.Update(row);
                updated++;
            }
            else
            {
                await _opinionRepository.AddAsync(new ShariahSourceOpinion
                {
                    StockId = stock.Id,
                    SourceKey = key,
                    Status = status,
                    Percentage = null,
                    Note = note,
                    PdfUrl = pdfUrl,
                    SourceLastUpdated = reportDate,
                    FetchedAt = now
                }, cancellationToken);
                inserted++;
            }
        }

        var stale = existingRows
            .Where(r =>
            {
                var ticker = r.Stock?.Ticker?.Trim();
                return string.IsNullOrEmpty(ticker) || !verdicts.ContainsKey(ticker);
            })
            .ToList();

        if (stale.Count > 0)
        {
            _opinionRepository.RemoveRange(stale);
        }

        return new ImportSourceOpinionsResult(
            key.ToString(),
            reportDate?.ToString("yyyy-MM-dd"),
            inserted,
            updated,
            stale.Count,
            boardListed.Count,
            unknown.OrderBy(t => t, StringComparer.Ordinal).ToList(),
            conflicts);
    }

    private static HashSet<string> Normalized(IEnumerable<string>? tickers)
        => new(
            (tickers ?? Enumerable.Empty<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpperInvariant()),
            StringComparer.OrdinalIgnoreCase);

    private static string? ResolvePath(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return File.Exists(requested) ? Path.GetFullPath(requested) : null;
        }

        var starts = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var start in starts)
        {
            var dir = new DirectoryInfo(start);
            for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, FileName);
                if (File.Exists(candidate)) return candidate;
            }
        }

        return null;
    }
}
