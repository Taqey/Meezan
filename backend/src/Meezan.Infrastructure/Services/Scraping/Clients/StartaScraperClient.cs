using AngleSharp;
using AngleSharp.Html.Dom;
using Meezan.Application.Common.Interfaces.Integrations;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Meezan.Infrastructure.Services.Scraping.Clients;

/// <summary>
/// Scrapes Starta Markets (https://startamarkets.com/companies) for EGX market data and daily quotes.
/// Replaces Mubasher as the primary price and quote scraper source.
/// </summary>
public class StartaScraperClient : IStockScraperClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StartaScraperClient> _logger;
    private readonly MubasherScraperClient _mubasherClient;

    private const string CompaniesUrl = "https://startamarkets.com/companies";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

    // In-memory cache for bulk runs so all 240+ stocks reuse the single table download
    private static readonly ConcurrentDictionary<string, StartaCompanyQuote> CachedQuotes = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _cacheTimestamp = DateTime.MinValue;
    private static readonly TimeSpan CacheValidity = TimeSpan.FromMinutes(2);
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    // Known symbol aliases from Starta to our Meezan DB tickers
    private static readonly Dictionary<string, string> StartaToDbAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "EGS385S1C012", "FERC" },
        { "AIH", "AIHC" },
        { "NDRL", "NDRL." },
        { "KORA", "KORA..." }
    };

    public record StartaCompanyQuote(
        string Symbol,
        string NameEn,
        string NameAr,
        string SectorName,
        decimal? Price,
        string? Currency,
        decimal? ChangePct,
        bool IsStale
    );

    public StartaScraperClient(
        HttpClient httpClient,
        ILogger<StartaScraperClient> logger,
        MubasherScraperClient mubasherClient)
    {
        _httpClient = httpClient;
        _logger = logger;
        _mubasherClient = mubasherClient;
    }

    /// <summary>
    /// Scrapes market data for one stock ticker from Starta Markets.
    /// Note: NAPR is fetched specifically from Mubasher as instructed.
    /// </summary>
    public async Task<ScrapedMarketData?> ScrapeMarketDataAsync(string ticker, CancellationToken ct = default)
    {
        if (ticker.Equals("NAPR", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("StartaScraperClient: Routing NAPR exclusively to MubasherScraperClient.");
            return await _mubasherClient.ScrapeMarketDataAsync(ticker, ct);
        }

        await EnsureQuotesLoadedAsync(ct);

        if (!TryGetQuote(ticker, out var quote))
        {
            _logger.LogWarning("Starta Markets: Stock {Ticker} not found in companies list.", ticker);
            return new ScrapedMarketData(ticker, null, null, null, null, null, null, null, null, null, null, null, null, IsNotFound: true);
        }

        // Return market data containing the latest price and quote status
        return new ScrapedMarketData(
            Ticker: ticker,
            NominalValue: null,
            MarketValue: null, // Market cap is computed by Meezan from price * shares
            BookValue: null,
            PbRatio: null,
            Eps: null,
            PeRatio: null,
            SharesOutstanding: null,
            Currency: quote.Currency ?? "EGP",
            High: null,
            Low: null,
            Open: null,
            ClosingPrice: quote.Price,
            SourceLastUpdateText: quote.IsStale ? "StaleQuote" : "StartaMarkets",
            IsNotFound: false
        );
    }

    /// <summary>
    /// Scrapes support/resistance & daily change levels for one stock ticker.
    /// Starta provides the official daily change percentage.
    /// Note: NAPR is fetched specifically from Mubasher as instructed.
    /// </summary>
    public async Task<ScrapedSupportResistance?> ScrapeSupportResistanceAsync(string ticker, CancellationToken ct = default)
    {
        if (ticker.Equals("NAPR", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("StartaScraperClient: Routing NAPR support/resistance exclusively to MubasherScraperClient.");
            return await _mubasherClient.ScrapeSupportResistanceAsync(ticker, ct);
        }

        await EnsureQuotesLoadedAsync(ct);

        if (!TryGetQuote(ticker, out var quote))
        {
            return null;
        }

        return new ScrapedSupportResistance(
            Ticker: ticker,
            LastPrice: quote.Price,
            ChangePct: quote.ChangePct ?? 0m,
            Pivot: null,
            R1: null,
            R2: null,
            S1: null,
            S2: null
        );
    }

    private bool TryGetQuote(string ticker, out StartaCompanyQuote quote)
    {
        // 1. Direct lookup
        if (CachedQuotes.TryGetValue(ticker, out quote!))
        {
            return true;
        }

        // 2. Reverse lookup in aliases (if caller passed a DB ticker or Starta symbol)
        foreach (var (startaSym, dbTicker) in StartaToDbAliases)
        {
            if (string.Equals(ticker, dbTicker, StringComparison.OrdinalIgnoreCase))
            {
                if (CachedQuotes.TryGetValue(startaSym, out quote!))
                {
                    return true;
                }
            }
        }

        quote = null!;
        return false;
    }

    private async Task EnsureQuotesLoadedAsync(CancellationToken ct)
    {
        if (DateTime.UtcNow - _cacheTimestamp < CacheValidity && !CachedQuotes.IsEmpty)
        {
            return;
        }

        await CacheLock.WaitAsync(ct);
        try
        {
            if (DateTime.UtcNow - _cacheTimestamp < CacheValidity && !CachedQuotes.IsEmpty)
            {
                return;
            }

            _logger.LogInformation("Refreshing Starta Markets company list from {Url}...", CompaniesUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, CompaniesUrl);
            request.Headers.Add("User-Agent", UserAgent);

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(ct);
            var parsed = await ParseStartaCompaniesHtmlAsync(html, ct);

            CachedQuotes.Clear();
            foreach (var item in parsed)
            {
                CachedQuotes[item.Symbol] = item;
                // Also index by canonical DB alias if known
                if (StartaToDbAliases.TryGetValue(item.Symbol, out var dbAlias))
                {
                    CachedQuotes[dbAlias] = item;
                }
            }

            _cacheTimestamp = DateTime.UtcNow;
            _logger.LogInformation("Successfully cached {Count} companies from Starta Markets.", CachedQuotes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load companies from Starta Markets: {Message}", ex.Message);
            throw;
        }
        finally
        {
            CacheLock.Release();
        }
    }

    public static async Task<List<StartaCompanyQuote>> ParseStartaCompaniesHtmlAsync(string html, CancellationToken ct)
    {
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(req => req.Content(html), ct) as IHtmlDocument;
        if (doc is null) return new List<StartaCompanyQuote>();

        var results = new List<StartaCompanyQuote>();
        var rows = doc.QuerySelectorAll("tbody tr");

        foreach (var row in rows)
        {
            var cols = row.QuerySelectorAll("td");
            if (cols.Length < 6) continue;

            // Starta table layout:
            // cols[0]: Index / rank (e.g. "1")
            // cols[1]: Names (English + Arabic)
            // cols[2]: Symbol (e.g. "COMI")
            // cols[3]: Sector (e.g. "Banks")
            // cols[4]: Price & Currency (e.g. "127.31", "2.53 USD", "—")
            // cols[5]: Change (e.g. "+1.18%", "-0.54%")
            // cols[6]: Market Cap (e.g. "430.8B")

            // Col 1 (index 1): Names
            var nameCol = cols[1];
            var nameArElem = nameCol.QuerySelector("[dir='rtl']");
            var nameAr = nameArElem?.TextContent?.Trim() ?? string.Empty;

            var fullText = nameCol.TextContent.Trim();
            var nameEn = !string.IsNullOrEmpty(nameAr)
                ? fullText.Replace(nameAr, "").Trim()
                : fullText;

            // Col 2 (index 2): Symbol
            var symLink = cols[2].QuerySelector("a");
            var symbol = (symLink?.TextContent ?? cols[2].TextContent).Trim();

            // Col 3 (index 3): Sector
            var sector = cols[3].TextContent.Trim();

            // Col 4 (index 4): Price & Currency
            var rawPrice = cols[4].TextContent.Trim();
            var (price, currency, isStalePrice) = ParsePriceAndCurrency(rawPrice);

            // Col 5 (index 5): Change
            var rawChange = cols[5].TextContent.Trim();
            var changePct = ParseChange(rawChange);

            results.Add(new StartaCompanyQuote(
                Symbol: symbol,
                NameEn: nameEn,
                NameAr: nameAr,
                SectorName: sector,
                Price: price,
                Currency: currency,
                ChangePct: isStalePrice ? 0m : changePct,
                IsStale: isStalePrice
            ));
        }

        return results;
    }

    private static (decimal? Price, string? Currency, bool IsStale) ParsePriceAndCurrency(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Contains("—"))
            return (null, null, true);

        // Strip unicode formatting marks like \u2066 and \u2069
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

        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
        {
            return (price, curr, false);
        }

        return (null, curr, true);
    }

    private static decimal? ParseChange(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Contains("—"))
            return 0m;

        var cleaned = raw.Replace("\u2066", "").Replace("\u2069", "").Replace("%", "").Replace("+", "").Trim();
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }

        return 0m;
    }
}

