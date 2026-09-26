using System.Text.Json;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Tools.IndexReimport;

/// <summary>
/// One-off, purely additive re-import of index constituents from the original
/// Excel/HTML source files sitting at the repo root.
///
/// Rules (per operator instruction):
///   * Parsing uses ExcelParserService — the exact same code path as the
///     Part 1 upload flow (header-name column matching, same weight-column
///     detection, first worksheet only, HTML-table fallback).
///   * Every row in the file is read as-is (ticker / names / weight).
///   * Missing Stock rows are created from whatever the file provides.
///   * Missing IndexConstituent links are created with the file's weight.
///     A file with no weight column yields Weight = NULL (never forced to 0).
///   * Existing links are NEVER touched — no overwrite, no recalculation.
///   * Nothing is ever deleted.
/// </summary>
public static class Program
{
    // File -> index code. EGX30-Capped_01-09-2026.xlsx is deliberately excluded
    // (no such index exists and its ticker set is identical to EGX30.xls).
    private static readonly (string File, string IndexCode)[] Mapping =
    [
        ("EGX30.xls", "EGX30"),
        ("EGX30TR.xls", "EGX30TR"),
        ("EGX70_EWI.xls", "EGX70"),
        ("EGX100_EWI.xls", "EGX100"),
        ("EGX35-LV-Constituents_research.xlsx", "EGX35-LV"),
        ("Shariah EGX 33.xlsx", "EGX 33"),
        ("Sectoral-Indices.xlsx", "Sectoral-Indices"),
        ("TAMAYUZ.xls", "TAMAYUZ"),
    ];

    public static async Task<int> Main(string[] args)
    {
        var dryRun = args.Any(a => a is "--dry-run" or "-n");
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var repoRoot = FindRepoRoot();
        var connection = ReadConnectionString(repoRoot);
        Console.WriteLine($"Repo root : {repoRoot}");
        Console.WriteLine($"Mode      : {(dryRun ? "DRY RUN (nothing will be written)" : "LIVE — additive insert only")}");
        Console.WriteLine();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        var parser = new ExcelParserService();

        var indices = await db.Indices.ToListAsync();
        var stocks = await db.Stocks.ToListAsync();

        var stocksByTicker = new Dictionary<string, Stock>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in stocks)
        {
            var key = s.Ticker.Trim();
            if (key.Length > 0 && !stocksByTicker.ContainsKey(key)) stocksByTicker[key] = s;
        }

        var sectors = await db.Sectors.ToListAsync();

        var results = new List<(string IndexCode, string File, int Before, int Added, int After, int StocksCreated, int SkippedRows, bool HasWeightColumn)>();
        var newStocks = new List<Stock>();

        foreach (var (fileName, indexCode) in Mapping)
        {
            var path = Path.Combine(repoRoot, fileName);
            if (!File.Exists(path))
            {
                Console.WriteLine($"!! file not found: {fileName}");
                results.Add((indexCode, fileName, 0, 0, 0, 0, 0, false));
                continue;
            }

            var index = indices.FirstOrDefault(i =>
                string.Equals(i.Code.Trim(), indexCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (index is null)
            {
                Console.WriteLine($"!! index '{indexCode}' not found in DB — skipped {fileName}");
                results.Add((indexCode, fileName, 0, 0, 0, 0, 0, false));
                continue;
            }

            await using var stream = File.OpenRead(path);
            var parsed = await parser.ParseIndexConstituentsAsync(stream, path);
            if (parsed.Count == 0)
            {
                Console.WriteLine($"!! no constituent rows parsed from {fileName}");
                results.Add((indexCode, fileName, 0, 0, 0, 0, 0, false));
                continue;
            }

            // "no weight column at all" → parser left Weight null for every row.
            var hasWeightColumn = parsed.Any(p => p.Weight.HasValue);

            var before = await db.IndexConstituents.CountAsync(c => c.IndexId == index.Id);
            var existingStockIds = (await db.IndexConstituents
                    .Where(c => c.IndexId == index.Id)
                    .Select(c => c.StockId)
                    .ToListAsync())
                .ToHashSet();

            var added = 0;
            var created = 0;
            var skippedRows = 0;
            var linkStockIds = new HashSet<int>(existingStockIds);

            await using var tx = await db.Database.BeginTransactionAsync();

            foreach (var row in parsed)
            {
                var ticker = string.IsNullOrWhiteSpace(row.Ticker)
                    ? string.Empty
                    : row.Ticker.Trim().ToUpperInvariant();

                if (ticker.Length == 0)
                {
                    skippedRows++;
                    continue;
                }

                // Sector only when the file actually provides one (same rule as the upload flow).
                Sector? sector = null;
                if (!string.IsNullOrWhiteSpace(row.SectorAr) || !string.IsNullOrWhiteSpace(row.SectorEn))
                {
                    sector = FindOrCreateSector(db, sectors, row.SectorAr, row.SectorEn, dryRun);
                }

                if (!stocksByTicker.TryGetValue(ticker, out var stock))
                {
                    stock = new Stock
                    {
                        Ticker = ticker,
                        NameAr = string.IsNullOrWhiteSpace(row.NameAr) ? ticker : row.NameAr.Trim(),
                        NameEn = string.IsNullOrWhiteSpace(row.NameEn) ? ticker : row.NameEn.Trim(),
                        SectorId = sector?.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.Stocks.Add(stock);
                    stocksByTicker[ticker] = stock;
                    newStocks.Add(stock);
                    created++;
                }

                if (linkStockIds.Contains(stock.Id))
                {
                    continue; // link already exists → leave completely untouched
                }

                db.IndexConstituents.Add(new IndexConstituent
                {
                    IndexId = index.Id,
                    Stock = stock,
                    Weight = row.Weight,                                  // null when the file has no weight column
                    EffectiveDate = row.EffectiveDate ?? DateTime.UtcNow.Date
                });
                linkStockIds.Add(stock.Id);
                added++;
            }

            if (dryRun)
            {
                await tx.RollbackAsync();
            }
            else
            {
                await db.SaveChangesAsync();
                if (added > 0)
                {
                    index.LastUpdated = DateTime.UtcNow;
                    db.Indices.Update(index);
                    await db.SaveChangesAsync();
                }
                await tx.CommitAsync();
            }

            var after = before + added;
            results.Add((index.Code, fileName, before, added, after, created, skippedRows, hasWeightColumn));

            Console.WriteLine($"{index.Code,-18} <- {fileName}");
            Console.WriteLine($"    rows parsed      : {parsed.Count}");
            Console.WriteLine($"    weight column    : {(hasWeightColumn ? "yes" : "NO  -> Weight = NULL")}");
            Console.WriteLine($"    links before     : {before}");
            Console.WriteLine($"    newly added      : {added}");
            Console.WriteLine($"    links after      : {after}");
            Console.WriteLine($"    stocks created   : {created}");
            if (skippedRows > 0) Console.WriteLine($"    rows skipped     : {skippedRows} (no ticker)");
            Console.WriteLine();
        }

        Console.WriteLine("==================== SUMMARY ====================");
        Console.WriteLine($"{"Index",-18} {"Before",7} {"Added",7} {"After",7} {"New stocks",11} {"File",-40}");
        foreach (var r in results)
        {
            Console.WriteLine($"{r.IndexCode,-18} {r.Before,7} {r.Added,7} {r.After,7} {r.StocksCreated,11} {r.File,-40}");
        }
        var totalBefore = results.Sum(r => r.Before);
        var totalAdded = results.Sum(r => r.Added);
        Console.WriteLine($"{"TOTAL",-18} {totalBefore,7} {totalAdded,7} {totalBefore + totalAdded,7}");
        Console.WriteLine();
        Console.WriteLine(dryRun
            ? "DRY RUN — no changes were written."
            : $"Committed. New stocks created: {newStocks.Count}.");

        return 0;
    }

    private static Sector? FindOrCreateSector(
        ApplicationDbContext db,
        List<Sector> sectors,
        string? nameAr,
        string? nameEn,
        bool dryRun)
    {
        var cleanAr = nameAr?.Trim();
        var cleanEn = nameEn?.Trim();
        if (string.IsNullOrWhiteSpace(cleanAr) && string.IsNullOrWhiteSpace(cleanEn)) return null;

        var existing = sectors.FirstOrDefault(s =>
            (!string.IsNullOrEmpty(cleanEn) && string.Equals(s.NameEn, cleanEn, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(cleanAr) && string.Equals(s.NameAr, cleanAr, StringComparison.Ordinal)));
        if (existing is not null) return existing;

        var sector = new Sector
        {
            NameAr = cleanAr ?? cleanEn ?? string.Empty,
            NameEn = cleanEn ?? cleanAr ?? string.Empty
        };
        if (!dryRun)
        {
            db.Sectors.Add(sector);
            db.SaveChanges();
        }
        sectors.Add(sector);
        return sector;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "backend", "src", "Meezan.WebApi", "appsettings.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    private static string ReadConnectionString(string repoRoot)
    {
        var appSettings = Path.Combine(repoRoot, "backend", "src", "Meezan.WebApi", "appsettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(appSettings));
        var conn = doc.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();
        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("DefaultConnection is missing from appsettings.json.");
        return conn;
    }
}
