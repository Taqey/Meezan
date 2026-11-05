using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;

namespace MeezanDbTool;

public static class ExecuteStartaMigration
{
    private const string ConnectionString = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    private static readonly Dictionary<string, string> SectorMap = new()
    {
        { "Banks", "البنوك" },
        { "Basic Resources", "المواد الأساسية" },
        { "Real Estate", "العقارات" },
        { "IT , Media & Communication Services", "التكنولوجيا والاتصالات" },
        { "Industrial Goods , Services and Automobiles", "الصناعات" },
        { "Non-bank financial services", "الخدمات المالية" },
        { "Food, Beverages and Tobacco", "الأغذية والمشروبات" },
        { "Health Care & Pharmaceuticals", "الرعاية الصحية" },
        { "Contracting & Construction Engineering", "المقاولات" },
        { "Shipping & Transportation Services", "النقل" },
        { "Building Materials", "مواد البناء" },
        { "Education Services", "التعليم" },
        { "Travel & Leisure", "السفر والترفيه" },
        { "Trade & Distributors", "التوزيع" },
        { "Textile & Durables", "السلع المعمرة" },
        { "Utilities", "المرافق" },
        { "Energy & Support Services", "الطاقة" },
        { "Paper & Packaging", "التعبئة والتغليف" }
    };

    private static readonly Dictionary<string, string> KnownAliases = new()
    {
        { "EGS385S1C012", "FERC" },
        { "TYCN", "ANFI" },
        { "EGS370O1C013", "NAPR" },
        { "AIH", "AIHC" },
        { "NDRL", "NDRL." },
        { "KORA", "KORA..." }
    };

    private record StartaItem(string Symbol, string NameEn, string NameAr, string RawSector, string RawPrice, string RawChange);
    private record StocksToFixRow(string Symbol, string SectorAr, decimal? FilePrice, decimal? FileMarketCap, decimal? Shares, string Action, string Note);

    public static async Task RunAsync()
    {
        Console.WriteLine("=== STARTING LIVE DATABASE UPDATE FROM STARTA MARKETS ===");

        // 1. Read Starta Parsed
        var jsonText = File.ReadAllText("tools/starta_parsed.json");
        var startaItems = JsonSerializer.Deserialize<List<StartaItem>>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Console.WriteLine($"Loaded {startaItems.Count} Starta items.");

        // 2. Read stocks_to_fix.csv
        var fixLines = File.ReadAllLines("stocks_to_fix.csv").Skip(1);
        var fixes = new List<StocksToFixRow>();
        foreach (var line in fixLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = ParseCsvLine(line);
            if (parts.Length < 6) continue;
            decimal.TryParse(parts[2], out var fp);
            decimal.TryParse(parts[3], out var fmc);
            decimal.TryParse(parts[4], out var sh);
            fixes.Add(new StocksToFixRow(parts[0].Trim(), parts[1].Trim(), fp > 0 ? fp : null, fmc > 0 ? fmc : null, sh > 0 ? sh : null, parts[5].Trim(), parts.Length > 6 ? parts[6].Trim() : ""));
        }
        Console.WriteLine($"Loaded {fixes.Count} fix rules from stocks_to_fix.csv.");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(ConnectionString);

        await using var db = new ApplicationDbContext(optionsBuilder.Options);
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var stocks = await db.Stocks
                    .Include(s => s.MarketData)
                    .Include(s => s.SupportResistance)
                    .Include(s => s.Sector)
                    .ToListAsync();

                var stockByTicker = stocks.ToDictionary(s => s.Ticker, StringComparer.OrdinalIgnoreCase);

                // Build Starta map by normalized ticker
                var startaBySymbol = new Dictionary<string, StartaItem>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in startaItems)
                {
                    var sym = item.Symbol.Trim();
                    if (KnownAliases.TryGetValue(sym, out var alias))
                    {
                        startaBySymbol[alias] = item;
                    }
                    else
                    {
                        startaBySymbol[sym] = item;
                    }
                }

                int updatedCount = 0;
                int staleCount = 0;
                int sharesFixedCount = 0;

                // A. Apply stocks_to_fix.csv set_shares
                foreach (var fix in fixes.Where(f => f.Action == "set_shares"))
                {
                    if (stockByTicker.TryGetValue(fix.Symbol, out var stock))
                    {
                        if (stock.MarketData == null)
                        {
                            stock.MarketData = new StockMarketData
                            {
                                StockId = stock.Id,
                                SharesOutstanding = fix.Shares,
                                IsSharesManual = true,
                                FetchedAt = DateTime.UtcNow
                            };
                            db.StockMarketData.Add(stock.MarketData);
                        }
                        else
                        {
                            stock.MarketData.SharesOutstanding = fix.Shares;
                            stock.MarketData.IsSharesManual = true;
                        }
                        sharesFixedCount++;
                        Console.WriteLine($"[stocks_to_fix] Set shares for {stock.Ticker}: {fix.Shares:N0} (IsSharesManual=true)");
                    }
                }

                // B. Update prices, daily change, and market cap from Starta
                var now = DateTime.UtcNow;

                foreach (var stock in stocks)
                {
                    if (startaBySymbol.TryGetValue(stock.Ticker, out var item))
                    {
                        var (price, curr) = ParsePrice(item.RawPrice);
                        var change = ParseChange(item.RawChange);

                        var md = stock.MarketData;
                        if (md == null)
                        {
                            md = new StockMarketData
                            {
                                StockId = stock.Id,
                                FetchedAt = now
                            };
                            stock.MarketData = md;
                            db.StockMarketData.Add(md);
                        }

                        var sr = stock.SupportResistance;
                        if (sr == null)
                        {
                            sr = new StockSupportResistance
                            {
                                StockId = stock.Id,
                                FetchedAt = now
                            };
                            stock.SupportResistance = sr;
                            db.StockSupportResistance.Add(sr);
                        }

                        if (price.HasValue)
                        {
                            // Active price quote today
                            md.ClosingPrice = price.Value;
                            if (!string.IsNullOrEmpty(curr))
                            {
                                md.Currency = curr;
                            }
                            md.FetchedAt = now;

                            // Update SupportResistance
                            sr.LastPrice = price.Value;
                            sr.ChangePct = change ?? 0m;
                            sr.FetchedAt = now;

                            // Update MarketValue = price * shares
                            if (md.SharesOutstanding.HasValue && md.SharesOutstanding.Value > 0)
                            {
                                md.MarketValue = md.ClosingPrice.Value * md.SharesOutstanding.Value;
                            }

                            updatedCount++;
                        }
                        else
                        {
                            // Stale quote '—' rule:
                            // keep existing price in DB, daily change = 0
                            staleCount++;
                            if (md.ClosingPrice == null || md.ClosingPrice == 0m)
                            {
                                // If DB has no price either, check file_price in stocks_to_fix
                                var f = fixes.FirstOrDefault(x => string.Equals(x.Symbol, stock.Ticker, StringComparison.OrdinalIgnoreCase));
                                if (f?.FilePrice != null)
                                {
                                    md.ClosingPrice = f.FilePrice.Value;
                                    Console.WriteLine($"[Stale] {stock.Ticker} had no DB price; filled from file_price: {f.FilePrice.Value}");
                                }
                            }

                            // Rule 3: daily change = 0
                            sr.ChangePct = 0m;
                            if (md.ClosingPrice.HasValue)
                            {
                                sr.LastPrice = md.ClosingPrice.Value;
                                if (md.SharesOutstanding.HasValue && md.SharesOutstanding.Value > 0)
                                {
                                    md.MarketValue = md.ClosingPrice.Value * md.SharesOutstanding.Value;
                                }
                            }
                            sr.FetchedAt = now;
                        }
                    }
                    else
                    {
                        // Stock in DB not on Starta: keep price, set change = 0
                        var sr = stock.SupportResistance;
                        if (sr != null)
                        {
                            sr.ChangePct = 0m;
                            sr.FetchedAt = now;
                        }
                    }
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                Console.WriteLine("=================================================");
                Console.WriteLine($"COMMIT SUCCESSFUL!");
                Console.WriteLine($"Updated prices & market caps: {updatedCount} stocks");
                Console.WriteLine($"Processed stale (change=0, price preserved): {staleCount} stocks");
                Console.WriteLine($"Shares explicitly updated via stocks_to_fix: {sharesFixedCount} stocks");
                Console.WriteLine("=================================================");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Console.WriteLine($"ERROR: Transaction rolled back: {ex.Message}");
                throw;
            }
        });
    }

    private static (decimal? Price, string? Currency) ParsePrice(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Contains("—"))
            return (null, null);

        var cleaned = raw.Replace("\u2066", "").Replace("\u2069", "").Replace(",", "").Trim();
        string? curr = null;
        if (cleaned.EndsWith("USD", StringComparison.OrdinalIgnoreCase))
        {
            curr = "USD";
            cleaned = cleaned.Replace("USD", "", StringComparison.OrdinalIgnoreCase).Trim();
        }
        else if (cleaned.EndsWith("EGP", StringComparison.OrdinalIgnoreCase))
        {
            curr = "EGP";
            cleaned = cleaned.Replace("EGP", "", StringComparison.OrdinalIgnoreCase).Trim();
        }

        if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
        {
            return (val, curr);
        }
        return (null, curr);
    }

    private static decimal? ParseChange(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Contains("—"))
            return 0m;

        var cleaned = raw.Replace("\u2066", "").Replace("\u2069", "").Replace("%", "").Replace("+", "").Trim();
        if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }
        return 0m;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
