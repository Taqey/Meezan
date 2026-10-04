using System.Text.Json;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Tools.ShariahAudit;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var conn = ReadConnectionString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(conn)
            .Options;

        await using var db = new ApplicationDbContext(options);

        Console.WriteLine("=============================================================");
        Console.WriteLine("  SHARIAH DATA INTEGRITY AUDIT");
        Console.WriteLine("=============================================================");
        Console.WriteLine();

        await AuditFaisalOsoulIsolation(db);
        Console.WriteLine();
        await AuditCompliantWithoutSource(db);
        Console.WriteLine();
        await AuditScraperSourceKeys(db);
        Console.WriteLine();
        await PrintSummaryStats(db);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SECTION 1: Faisal / Osoul isolation snapshot
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task AuditFaisalOsoulIsolation(ApplicationDbContext db)
    {
        Console.WriteLine("--- SECTION 1: Faisal/Osoul opinion rows (snapshot) ---");

        var faisalRows = await db.ShariahSourceOpinions
            .Include(o => o.Stock)
            .Where(o => o.SourceKey == ShariahSourceKey.FaisalBank)
            .OrderBy(o => o.Stock!.Ticker)
            .ToListAsync();

        var osoulRows = await db.ShariahSourceOpinions
            .Include(o => o.Stock)
            .Where(o => o.SourceKey == ShariahSourceKey.Osoul)
            .OrderBy(o => o.Stock!.Ticker)
            .ToListAsync();

        Console.WriteLine($"  FaisalBank opinion rows : {faisalRows.Count}");
        foreach (var r in faisalRows)
            Console.WriteLine($"    [{r.Stock?.Ticker,-10}] status={r.Status,-15} pdfUrl={(r.PdfUrl != null ? "set" : "null")} fetchedAt={r.FetchedAt:yyyy-MM-dd} note={r.Note?.Substring(0, Math.Min(60, r.Note?.Length ?? 0))}");

        Console.WriteLine($"  Osoul opinion rows      : {osoulRows.Count}");
        foreach (var r in osoulRows)
            Console.WriteLine($"    [{r.Stock?.Ticker,-10}] status={r.Status,-15} pdfUrl={(r.PdfUrl != null ? "set" : "null")} fetchedAt={r.FetchedAt:yyyy-MM-dd} note={r.Note?.Substring(0, Math.Min(60, r.Note?.Length ?? 0))}");

        // Verify: all rows should have notes matching manual-import pattern
        int suspiciousRows = 0;
        foreach (var r in faisalRows.Concat(osoulRows))
        {
            // Manual import notes always contain "استيراد يدوي من تقرير"
            bool isManualImportNote = r.Note != null && r.Note.Contains("استيراد يدوي من تقرير");
            bool hasNote = r.Note != null;
            if (hasNote && !isManualImportNote)
            {
                Console.WriteLine($"  WARNING SUSPICIOUS ROW: {r.Stock?.Ticker} / {r.SourceKey} note='{r.Note}' does not match manual-import pattern");
                suspiciousRows++;
            }
            else if (!hasNote)
            {
                Console.WriteLine($"  INFO NULL-NOTE ROW: {r.Stock?.Ticker} / {r.SourceKey} (status={r.Status})");
            }
        }

        if (suspiciousRows == 0)
            Console.WriteLine("  PASS: All Faisal/Osoul rows have manual-import notes - scraper isolation OK.");
        else
            Console.WriteLine($"  FAIL: {suspiciousRows} suspicious Faisal/Osoul row(s) detected - investigate!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SECTION 2: Compliant stocks with no supporting source
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task AuditCompliantWithoutSource(ApplicationDbContext db)
    {
        Console.WriteLine("--- SECTION 2: Compliant stocks with no supporting source ---");

        var stocks = await db.Stocks
            .Include(s => s.ShariahCompliance)
            .Include(s => s.ShariahMetrics)
            .Include(s => s.ShariahSourceOpinions)
            .Include(s => s.IndexConstituents)
                .ThenInclude(ic => ic.Index)
            .Where(s => s.ShariahCompliance != null && s.ShariahCompliance.Status == ShariahStatus.Compliant)
            .ToListAsync();

        Console.WriteLine($"  Stocks with Status=Compliant: {stocks.Count}");

        var staleActivityGate = new List<Stock>();
        var noSource = new List<(Stock stock, string opinions)>();

        foreach (var stock in stocks)
        {
            var compliance = stock.ShariahCompliance!;
            var metrics = stock.ShariahMetrics;
            var opinions = stock.ShariahSourceOpinions;

            // Activity gate: CoreActivityCompliant==false means status should be NonCompliant
            if (metrics?.CoreActivityCompliant == false)
            {
                staleActivityGate.Add(stock);
                continue;
            }

            // Check supporting source
            bool hasCompliantOpinion = opinions.Any(o =>
                o.Status != null && o.Status.ToUpper() == "COMPLIANT");

            bool inEgx33 = stock.IndexConstituents.Any(ic =>
                ic.Index != null &&
                (string.Equals(ic.Index.Code, "Shariah", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(ic.Index.Code, "EGX33", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(ic.Index.Code, "EGX 33", StringComparison.OrdinalIgnoreCase)));

            bool hasBoard = compliance.HasShariahBoard;

            if (!hasCompliantOpinion && !inEgx33 && !hasBoard)
            {
                var opinionSummary = opinions.Count > 0
                    ? string.Join(", ", opinions.Select(o => $"{o.SourceKey}={o.Status ?? "null"}"))
                    : "(no opinions)";
                noSource.Add((stock, opinionSummary));
            }
        }

        // Report stale activity-gate rows
        if (staleActivityGate.Count > 0)
        {
            Console.WriteLine($"  FAIL: {staleActivityGate.Count} stock(s) are Compliant but CoreActivityCompliant=false (stale - need scraper re-run):");
            foreach (var s in staleActivityGate.OrderBy(x => x.Ticker))
                Console.WriteLine($"    [{s.Ticker,-10}] {s.NameAr}");
        }

        // Report compliant with no source
        if (noSource.Count == 0)
        {
            Console.WriteLine("  PASS: All Compliant stocks have at least one supporting source (board opinion, EGX33, or HasShariahBoard).");
        }
        else
        {
            Console.WriteLine($"  FAIL: {noSource.Count} stock(s) are Compliant with NO supporting source:");
            foreach (var (stock, opinionSummary) in noSource.OrderBy(x => x.stock.Ticker))
                Console.WriteLine($"    [{stock.Ticker,-10}] {stock.NameAr,-30} opinions: [{opinionSummary}]");
        }

        if (staleActivityGate.Count == 0 && noSource.Count == 0)
            Console.WriteLine("  PASS: No integrity violations found.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SECTION 3: Opinion row counts by source key
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task AuditScraperSourceKeys(ApplicationDbContext db)
    {
        Console.WriteLine("--- SECTION 3: Opinion row counts by source key ---");

        var groups = await db.ShariahSourceOpinions
            .GroupBy(o => o.SourceKey)
            .Select(g => new
            {
                SourceKey = g.Key,
                Total = g.Count(),
                Compliant = g.Count(o => o.Status != null && o.Status.ToUpper() == "COMPLIANT"),
                NonCompliant = g.Count(o => o.Status != null &&
                    (o.Status.ToUpper() == "NON_COMPLIANT" || o.Status.ToUpper() == "NONCOMPLIANT"))
            })
            .ToListAsync();

        Console.WriteLine($"  {"SourceKey",-20} {"Total",7} {"Compliant",10} {"NonCompliant",13}  Type");
        Console.WriteLine($"  {new string('-', 70)}");
        foreach (var g in groups.OrderBy(x => (int)x.SourceKey))
        {
            var type = (g.SourceKey == ShariahSourceKey.FaisalBank || g.SourceKey == ShariahSourceKey.Osoul)
                ? "PDF-ONLY (manual import)"
                : "scraper";
            Console.WriteLine($"  {g.SourceKey,-20} {g.Total,7} {g.Compliant,10} {g.NonCompliant,13}  {type}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SECTION 4: Summary statistics
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task PrintSummaryStats(ApplicationDbContext db)
    {
        Console.WriteLine("--- SECTION 4: Summary statistics ---");

        var totalStocks = await db.Stocks.CountAsync();
        var activeStocks = await db.Stocks.CountAsync(s => s.IsActive);
        var hasCompliance = await db.ShariahCompliances.CountAsync();
        var compliantCount = await db.ShariahCompliances.CountAsync(c => c.Status == ShariahStatus.Compliant);
        var nonCompliantCount = await db.ShariahCompliances.CountAsync(c => c.Status == ShariahStatus.NonCompliant);
        var pendingCount = await db.ShariahCompliances.CountAsync(c => c.Status == ShariahStatus.Pending);
        var blockedCount = await db.ShariahCompliances.CountAsync(c => c.Status == ShariahStatus.Blocked);
        var noCompliance = totalStocks - hasCompliance;
        var totalOpinions = await db.ShariahSourceOpinions.CountAsync();

        Console.WriteLine($"  Total stocks             : {totalStocks}");
        Console.WriteLine($"  Active stocks            : {activeStocks}");
        Console.WriteLine($"  Stocks with compliance   : {hasCompliance}");
        Console.WriteLine($"  Stocks without compliance: {noCompliance}");
        Console.WriteLine();
        Console.WriteLine($"  Compliance status breakdown:");
        Console.WriteLine($"    Compliant     : {compliantCount}");
        Console.WriteLine($"    NonCompliant  : {nonCompliantCount}");
        Console.WriteLine($"    Pending       : {pendingCount}");
        Console.WriteLine($"    Blocked       : {blockedCount}");
        Console.WriteLine();
        Console.WriteLine($"  Total ShariahSourceOpinion rows: {totalOpinions}");
    }

    private static string ReadConnectionString()
    {
        var starts = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var start in starts)
        {
            var dir = new DirectoryInfo(start);
            for (int i = 0; i < 10 && dir != null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "backend", "src", "Meezan.WebApi", "appsettings.json");
                if (File.Exists(candidate))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(candidate));
                    return doc.RootElement
                        .GetProperty("ConnectionStrings")
                        .GetProperty("DefaultConnection")
                        .GetString()!;
                }
            }
        }

        throw new FileNotFoundException("Could not find appsettings.json. Run the tool from within the Meezan workspace directory.");
    }
}
