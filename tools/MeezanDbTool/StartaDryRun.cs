using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;

namespace MeezanDbTool;

public class StartaDryRun
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public class StartaCompany
    {
        public string Symbol { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameAr { get; set; } = "";
        public string RawSector { get; set; } = "";
        public string RawPrice { get; set; } = "";
        public string RawChange { get; set; } = "";
        public decimal? ParsedPrice { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal? ParsedChangePct { get; set; }
        public bool IsNoTradeOrStale { get; set; }
    }

    public class FixStockItem
    {
        public string Symbol { get; set; } = "";
        public string SectorAr { get; set; } = "";
        public decimal FilePrice { get; set; }
        public decimal FileMarketCap { get; set; }
        public decimal Shares { get; set; }
        public string Action { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public static readonly Dictionary<string, string> SectorMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Banks", "البنوك" },
        { "Basic Resources", "المواد الأساسية" },
        { "Real Estate", "العقارات" },
        { "IT , Media & Communication Services", "التكنولوجيا والاتصالات" },
        { "IT , Media &amp; Communication Services", "التكنولوجيا والاتصالات" },
        { "Industrial Goods , Services and Automobiles", "الصناعات" },
        { "Non-bank financial services", "الخدمات المالية" },
        { "Food, Beverages and Tobacco", "الأغذية والمشروبات" },
        { "Health Care & Pharmaceuticals", "الرعاية الصحية" },
        { "Health Care &amp; Pharmaceuticals", "الرعاية الصحية" },
        { "Contracting & Construction Engineering", "المقاولات" },
        { "Contracting &amp; Construction Engineering", "المقاولات" },
        { "Shipping & Transportation Services", "النقل" },
        { "Shipping &amp; Transportation Services", "النقل" },
        { "Building Materials", "مواد البناء" },
        { "Education Services", "التعليم" },
        { "Travel & Leisure", "السفر والترفيه" },
        { "Trade & Distributors", "التوزيع" },
        { "Textile & Durables", "السلع المعمرة" },
        { "Utilities", "المرافق" },
        { "Energy & Support Services", "الطاقة" },
        { "Energy &amp; Support Services", "الطاقة" },
        { "Paper & Packaging", "التعبئة والتغليف" }
    };

    public static async Task RunAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== STARTA DRY-RUN ANALYSIS ===");

        // 1. Parse Starta HTML
        string html = "";
        if (File.Exists("tools/starta_page.html"))
        {
            html = File.ReadAllText("tools/starta_page.html");
        }
        else
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            html = await client.GetStringAsync("https://startamarkets.com/companies");
        }

        var startaList = ParseStartaHtml(html);
        Console.WriteLine($"Parsed {startaList.Count} companies from Starta Markets.");

        // 2. Read stocks_to_fix.csv
        var fixItems = ReadStocksToFix("stocks_to_fix.csv");
        Console.WriteLine($"Loaded {fixItems.Count} items from stocks_to_fix.csv.");

        // 3. Connect to DB
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnStr)
            .Options;
        using var db = new ApplicationDbContext(options);

        var dbStocks = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Include(s => s.Sector)
            .ToListAsync();

        Console.WriteLine($"Loaded {dbStocks.Count} stocks from DB.");

        // 4. Analysis and Dry-Run
        var startaMap = startaList.ToDictionary(s => s.Symbol.ToUpperInvariant(), s => s);
        var dbMap = dbStocks.ToDictionary(s => s.Ticker.ToUpperInvariant(), s => s);

        // Track categories
        var matchedUpdates = new List<(Stock dbStock, StartaCompany starta, decimal oldPrice, decimal newPrice, decimal oldChg, decimal newChg, decimal? relDiff)>();
        var keptAsIs = new List<(Stock dbStock, StartaCompany starta, string reason)>();
        var dbStocksNotOnStarta = new List<Stock>();
        var startaNotOnDb = new List<StartaCompany>();
        var proposedAliases = new List<(StartaCompany starta, Stock candidate, string reason)>();
        var confirmedMissing = new List<StartaCompany>();
        var largeDeviations = new List<(string Symbol, decimal oldPrice, decimal newPrice, decimal pctDiff)>();

        // Check DB stocks against Starta
        foreach (var s in dbStocks)
        {
            var ticker = s.Ticker.ToUpperInvariant();
            if (startaMap.TryGetValue(ticker, out var sc))
            {
                decimal oldPrice = s.MarketData?.ClosingPrice ?? 0m;
                decimal oldChg = s.SupportResistance?.ChangePct ?? 0m;

                if (sc.IsNoTradeOrStale || !sc.ParsedPrice.HasValue)
                {
                    // Rule 3: keep old price, change = 0
                    keptAsIs.Add((s, sc, "Starta has no quote / stale (—)"));
                }
                else
                {
                    decimal newPrice = sc.ParsedPrice.Value;
                    decimal newChg = sc.ParsedChangePct ?? 0m;
                    decimal? relDiff = oldPrice > 0 ? Math.Abs((newPrice - oldPrice) / oldPrice * 100m) : (decimal?)null;

                    if (relDiff.HasValue && relDiff.Value > 20m)
                    {
                        largeDeviations.Add((ticker, oldPrice, newPrice, Math.Round(relDiff.Value, 1)));
                    }

                    matchedUpdates.Add((s, sc, oldPrice, newPrice, oldChg, newChg, relDiff));
                }
            }
            else
            {
                dbStocksNotOnStarta.Add(s);
            }
        }

        // Check Starta companies not in DB
        foreach (var sc in startaList)
        {
            var sym = sc.Symbol.ToUpperInvariant();
            if (!dbMap.ContainsKey(sym))
            {
                startaNotOnDb.Add(sc);

                // Look for alternative symbol or company name
                var candidate = dbStocks.FirstOrDefault(s =>
                    string.Equals(s.Ticker.Replace(".", ""), sym.Replace(".", ""), StringComparison.OrdinalIgnoreCase) ||
                    (s.NameAr != null && sc.NameAr != null && (s.NameAr.Contains(sc.NameAr) || sc.NameAr.Contains(s.NameAr))) ||
                    (s.NameEn != null && sc.NameEn != null && (s.NameEn.Contains(sc.NameEn) || sc.NameEn.Contains(s.NameEn))));

                if (candidate != null)
                {
                    proposedAliases.Add((sc, candidate, $"Symbol match or Name match ({candidate.Ticker})"));
                }
                else
                {
                    confirmedMissing.Add(sc);
                }
            }
        }

        // Process stocks_to_fix.csv
        var setSharesReport = new List<(FixStockItem fix, Stock? dbStock, string status)>();
        var findOrInsertReport = new List<(FixStockItem fix, StartaCompany? starta, Stock? candidate, string status)>();

        foreach (var fix in fixItems)
        {
            if (fix.Action == "set_shares")
            {
                if (dbMap.TryGetValue(fix.Symbol.ToUpperInvariant(), out var stock))
                {
                    setSharesReport.Add((fix, stock, "Found in DB, will update SharesOutstanding only"));
                }
                else
                {
                    setSharesReport.Add((fix, null, "NOT found in DB! (Cannot set shares)"));
                }
            }
            else if (fix.Action == "find_or_insert")
            {
                startaMap.TryGetValue(fix.Symbol.ToUpperInvariant(), out var sc);
                var cand = dbStocks.FirstOrDefault(s =>
                    string.Equals(s.Ticker.Replace(".", ""), fix.Symbol.Replace(".", "").Replace("…", ""), StringComparison.OrdinalIgnoreCase));

                string status = sc != null ? "Found on Starta" : "NOT on Starta (File price preserved)";
                findOrInsertReport.Add((fix, sc, cand, status));
            }
        }

        // Output Comprehensive Dry-Run
        Console.WriteLine();
        Console.WriteLine("==========================================================================");
        Console.WriteLine($"1. MATCHED & READY TO UPDATE: {matchedUpdates.Count} stocks");
        Console.WriteLine($"2. KEPT AS-IS (Stale / No Trade '—'): {keptAsIs.Count} stocks");
        Console.WriteLine($"3. DB STOCKS NOT ON STARTA: {dbStocksNotOnStarta.Count} stocks");
        Console.WriteLine($"4. STARTA STOCKS NOT IN DB: {startaNotOnDb.Count} (Proposed Aliases: {proposedAliases.Count}, Truly New: {confirmedMissing.Count})");
        Console.WriteLine($"5. STOCKS WITH PRICE DEVIATION > 20%: {largeDeviations.Count}");
        Console.WriteLine($"6. STOCKS_TO_FIX.CSV ACTIONS: {fixItems.Count} (set_shares: {setSharesReport.Count}, find_or_insert: {findOrInsertReport.Count})");
        Console.WriteLine("==========================================================================");

        if (largeDeviations.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("--- STOCKS WITH PRICE DEVIATION > 20% ---");
            foreach (var d in largeDeviations.OrderByDescending(x => x.pctDiff))
            {
                Console.WriteLine($"{d.Symbol,-10} | Old Price: {d.oldPrice,8:N2} | Starta Price: {d.newPrice,8:N2} | Deviation: {d.pctDiff}%");
            }
        }

        if (proposedAliases.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("--- PROPOSED ALIASES ---");
            foreach (var pa in proposedAliases)
            {
                Console.WriteLine($"Starta Symbol: {pa.starta.Symbol,-10} ({pa.starta.NameAr}) -> DB Candidate: {pa.candidate.Ticker} ({pa.candidate.NameAr}) | Reason: {pa.reason}");
            }
        }

        if (confirmedMissing.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("--- TRULY NEW STOCKS ON STARTA (Candidates for Insert) ---");
            foreach (var cm in confirmedMissing)
            {
                string sectorAr = SectorMapping.TryGetValue(cm.RawSector, out var sa) ? sa : cm.RawSector;
                Console.WriteLine($"Symbol: {cm.Symbol,-10} | Name: {cm.NameAr} | Sector: {sectorAr} | Price: {cm.ParsedPrice} {cm.Currency} | Change: {cm.ParsedChangePct}%");
            }
        }

        if (dbStocksNotOnStarta.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("--- DB STOCKS NOT ON STARTA ---");
            foreach (var ds in dbStocksNotOnStarta.Take(15))
            {
                Console.WriteLine($"DB Ticker: {ds.Ticker,-10} | Name: {ds.NameAr} | Price: {ds.MarketData?.ClosingPrice}");
            }
            if (dbStocksNotOnStarta.Count > 15) Console.WriteLine($"... and {dbStocksNotOnStarta.Count - 15} more.");
        }

        Console.WriteLine();
        Console.WriteLine("--- STOCKS_TO_FIX.CSV REPORT ---");
        foreach (var r in setSharesReport)
        {
            Console.WriteLine($"[set_shares] {r.fix.Symbol,-8} | Shares: {r.fix.Shares,14:N0} | Status: {r.status}");
        }
        foreach (var r in findOrInsertReport)
        {
            Console.WriteLine($"[find_or_insert] {r.fix.Symbol,-8} | FilePrice: {r.fix.FilePrice} | Status: {r.status} | Candidate: {r.candidate?.Ticker ?? "None"}");
        }
    }

    public static List<StartaCompany> ParseStartaHtml(string html)
    {
        var list = new List<StartaCompany>();
        int tbodyStart = html.IndexOf("<tbody");
        int tbodyEnd = html.IndexOf("</tbody>");
        if (tbodyStart == -1 || tbodyEnd == -1) return list;

        string tbody = html.Substring(tbodyStart, tbodyEnd - tbodyStart);
        var rows = tbody.Split(new[] { "<tr" }, StringSplitOptions.RemoveEmptyEntries).Skip(1);

        foreach (var row in rows)
        {
            var symMatch = Regex.Match(row, @"href=""\/symbol\/([^""]+)""");
            if (!symMatch.Success) continue;
            string symbol = symMatch.Groups[1].Value.Trim();

            var arMatch = Regex.Match(row, @"dir=""rtl""[^>]*>([^<]+)<");
            string nameAr = arMatch.Success ? arMatch.Groups[1].Value.Trim() : "";

            var enMatch = Regex.Match(row, @"href=""\/symbol\/[^""]+"">([^<]+)<\/a>");
            string nameEn = enMatch.Success ? enMatch.Groups[1].Value.Trim() : "";

            var cellMatches = Regex.Matches(row, @"<td[^>]*>([\s\S]*?)<\/td>");
            // Cells: 0: Pos, 1: Company, 2: Symbol, 3: Sector, 4: Price, 5: Change, 6: Market Cap
            string rawSector = cellMatches.Count > 3 ? StripHtml(cellMatches[3].Groups[1].Value) : "";
            string rawPrice = cellMatches.Count > 4 ? StripHtml(cellMatches[4].Groups[1].Value) : "";
            string rawChange = cellMatches.Count > 5 ? StripHtml(cellMatches[5].Groups[1].Value) : "";

            // Parse price and currency
            // Clean bidirectional markers: ⁦, ⁩
            string cleanPrice = CleanBidi(rawPrice);
            string cleanChange = CleanBidi(rawChange);

            bool isStale = cleanPrice.Contains("—") || cleanChange.Contains("—");
            string currency = cleanPrice.Contains("USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "EGP";

            decimal? priceVal = null;
            if (!isStale)
            {
                var numMatch = Regex.Match(cleanPrice, @"[\d\.]+");
                if (numMatch.Success && decimal.TryParse(numMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                {
                    priceVal = p;
                }
            }

            decimal? changeVal = null;
            if (!isStale)
            {
                var chgMatch = Regex.Match(cleanChange, @"[+-]?[\d\.]+");
                if (chgMatch.Success && decimal.TryParse(chgMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var c))
                {
                    changeVal = c;
                }
            }
            else
            {
                changeVal = 0m; // Rule 3: stale change = 0
            }

            list.Add(new StartaCompany
            {
                Symbol = symbol,
                NameAr = nameAr,
                NameEn = nameEn,
                RawSector = rawSector,
                RawPrice = rawPrice,
                RawChange = rawChange,
                ParsedPrice = priceVal,
                Currency = currency,
                ParsedChangePct = changeVal,
                IsNoTradeOrStale = isStale
            });
        }

        return list;
    }

    private static string CleanBidi(string text)
    {
        return text.Replace("⁦", "").Replace("⁩", "").Trim();
    }

    private static string StripHtml(string html)
    {
        return Regex.Replace(html, @"<[^>]+>", "").Trim();
    }

    private static List<FixStockItem> ReadStocksToFix(string filePath)
    {
        var list = new List<FixStockItem>();
        if (!File.Exists(filePath)) return list;

        var lines = File.ReadAllLines(filePath).Skip(1);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // Parse CSV considering quoted notes
            var parts = Regex.Split(line, @",(?=(?:[^""]*""[^""]*"")*[^""]*$)");
            if (parts.Length >= 6)
            {
                list.Add(new FixStockItem
                {
                    Symbol = parts[0].Trim(),
                    SectorAr = parts[1].Trim(),
                    FilePrice = decimal.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fp) ? fp : 0m,
                    FileMarketCap = decimal.TryParse(parts[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fmc) ? fmc : 0m,
                    Shares = decimal.TryParse(parts[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var sh) ? sh : 0m,
                    Action = parts[5].Trim(),
                    Note = parts.Length > 6 ? parts[6].Trim().Trim('"') : ""
                });
            }
        }
        return list;
    }
}
