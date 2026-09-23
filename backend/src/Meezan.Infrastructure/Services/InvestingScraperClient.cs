using AngleSharp;
using AngleSharp.Html.Dom;
using Meezan.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Meezan.Infrastructure.Services;

/// <summary>
/// Fallback scraper that fetches individual missing market-data fields from investing.com.
/// Called ONLY when Mubasher returned null for one or more fields — never replaces a
/// field that Mubasher already populated.
///
/// IMPORTANT: investing.com's Terms of Service restrict automated scraping.
/// Verify compliance before deploying this to production.
/// </summary>
public partial class InvestingScraperClient : IInvestingScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InvestingScraperClient> _logger;

    // investing.com Egyptian stock pages follow this pattern:
    //   https://www.investing.com/equities/{slug}
    // where slug is usually the lowercase company name with hyphens.
    // The mapping below covers the most common EGX tickers.
    // Add more entries as needed; unknown tickers fall back to a slug derived from the ticker.
    private static readonly Dictionary<string, string> TickerToSlug = new(StringComparer.OrdinalIgnoreCase)
    {
        { "COMI",  "commercial-international-bank-egypt" },
        { "EAST",  "eastern-company" },
        { "ETEL",  "telecom-egypt" },
        { "AMOC",  "alexandria-mineral-oils" },
        { "SWDY",  "el-sewedy-electric" },
        { "HRHO",  "heliopolis-housing" },
        { "EKHO",  "extracted-toner-powders" },
        { "FWRY",  "fawry-for-banking-technology-and-electronic-payments" },
        { "TMGH",  "talaat-moustafa" },
        { "EGCH",  "egyptian-chemical-industries" },
        { "ORWE",  "oriental-weavers" },
        { "GTHE",  "ghabbour-auto" },
        { "JUFO",  "juhayna-food-industries" },
        { "PHDC",  "palm-hills-developments" },
        { "EFID",  "egyptian-financial-group-hermes" },
        { "EFGH",  "efg-hermes" },
        { "MNHD",  "madinet-nasr-for-housing" },
        { "SKPC",  "sidi-kerir-petrochemicals" },
        { "ESRS",  "ezz-steel" },
        { "ALCN",  "alex-cement" },
        { "ABUK",  "abu-dhabi-islamic-bank-egypt" },
        { "ADIB",  "abu-dhabi-islamic-bank-egypt" },
        { "CIEB",  "cib-egypt" },
        { "HELI",  "helwan-cement" },
    };

    private const string BaseUrl = "https://www.investing.com/equities/";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

    // Regex to strip currency symbols and commas before parsing
    private static readonly Regex NumberPattern = new(@"[-+]?[\d,]+\.?\d*", RegexOptions.Compiled);

    public InvestingScraperClient(HttpClient httpClient, ILogger<InvestingScraperClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScrapedMarketData> FillMissingFieldsAsync(
        ScrapedMarketData primary,
        CancellationToken ct = default)
    {
        // If none of the in-scope fields are missing, skip the call entirely
        bool anyMissing =
            primary.PeRatio      is null ||
            primary.PbRatio      is null ||
            primary.Eps          is null ||
            primary.BookValue    is null ||
            primary.MarketValue  is null ||
            primary.High         is null ||
            primary.Low          is null ||
            primary.Open         is null ||
            primary.ClosingPrice is null;

        if (!anyMissing)
            return primary;

        var slug = ResolveSlug(primary.Ticker);
        var url  = BaseUrl + slug;

        _logger.LogInformation(
            "Investing.com fallback: fetching missing fields for {Ticker} from {Url}",
            primary.Ticker, url);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Investing.com returned HTTP {Status} for {Ticker} ({Url})",
                    (int)response.StatusCode, primary.Ticker, url);
                return primary;
            }

            var html = await response.Content.ReadAsStringAsync(ct);
            return await ParseAndFillAsync(primary, html, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                "Investing.com fallback failed for {Ticker}: {Error}",
                primary.Ticker, ex.Message);
            return primary;
        }
    }

    // ── Slug resolution ────────────────────────────────────────────────────────

    private static string ResolveSlug(string ticker)
    {
        if (TickerToSlug.TryGetValue(ticker, out var slug))
            return slug;

        // Best-effort: use lowercase ticker as slug — many Egyptian stocks
        // are reachable at /equities/{ticker-lowercase}
        return ticker.ToLowerInvariant();
    }

    // ── HTML parsing ───────────────────────────────────────────────────────────

    private static async Task<ScrapedMarketData> ParseAndFillAsync(
        ScrapedMarketData primary, string html, CancellationToken ct)
    {
        var config  = Configuration.Default;
        var context = BrowsingContext.New(config);
        var doc     = await context.OpenAsync(req => req.Content(html), ct) as IHtmlDocument;
        if (doc is null) return primary;

        // investing.com renders key/value pairs inside elements with data-test attributes
        // and also in summary table rows of the form:
        //   <td class="key">P/E Ratio</td><td class="value">18.5</td>
        //
        // Strategy: collect all visible text, then scan for known English labels.

        var text = doc.Body?.TextContent ?? string.Empty;
        var lines = text
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var parsed = ParseFieldsFromLines(lines);

        // Per-field fallback: only overwrite if Mubasher returned null
        return primary with
        {
            PeRatio      = primary.PeRatio      ?? parsed.GetValueOrDefault("pe"),
            PbRatio      = primary.PbRatio      ?? parsed.GetValueOrDefault("pb"),
            Eps          = primary.Eps          ?? parsed.GetValueOrDefault("eps"),
            BookValue    = primary.BookValue    ?? parsed.GetValueOrDefault("bookvalue"),
            MarketValue  = primary.MarketValue  ?? parsed.GetValueOrDefault("marketcap"),
            High         = primary.High         ?? parsed.GetValueOrDefault("high"),
            Low          = primary.Low          ?? parsed.GetValueOrDefault("low"),
            Open         = primary.Open         ?? parsed.GetValueOrDefault("open"),
            ClosingPrice = primary.ClosingPrice ?? parsed.GetValueOrDefault("price"),
        };
    }

    /// <summary>
    /// Scans a list of trimmed text lines for known investing.com field labels
    /// and returns a key->decimal map of parsed values.
    /// </summary>
    private static Dictionary<string, decimal?> ParseFieldsFromLines(List<string> lines)
    {
        var result = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

        // Label-to-key mapping (English labels as they appear on investing.com)
        var labelMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "P/E Ratio",             "pe"        },
            { "Price/Earnings",        "pe"        },
            { "Price To Earnings",     "pe"        },
            { "P/B Ratio",             "pb"        },
            { "Price/Book",            "pb"        },
            { "Price To Book",         "pb"        },
            { "EPS",                   "eps"       },
            { "Earnings Per Share",    "eps"       },
            { "Book Value Per Share",  "bookvalue" },
            { "Market Cap",            "marketcap" },
            { "52-Week High",          "high"      },
            { "1-Year High",           "high"      },
            { "52-Week Low",           "low"       },
            { "1-Year Low",            "low"       },
            { "Open",                  "open"      },
            { "Today's Open",          "open"      },
            { "Price",                 "price"     },
            { "Last Price",            "price"     },
        };

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            foreach (var (label, key) in labelMap)
            {
                if (!line.Equals(label, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (result.ContainsKey(key))
                    break; // first occurrence wins

                // The value is expected on the next non-empty line
                for (int j = i + 1; j < Math.Min(i + 4, lines.Count); j++)
                {
                    var candidate = lines[j];
                    var num = ParseNumber(candidate);
                    if (num.HasValue)
                    {
                        result[key] = num;
                        break;
                    }
                }
                break;
            }
        }

        return result;
    }

    private static decimal? ParseNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Strip known non-numeric suffixes (M, B, T for millions/billions)
        var scaled = 1m;
        var clean  = raw.Trim().TrimEnd('%');
        if (clean.EndsWith("M", StringComparison.OrdinalIgnoreCase)) { clean = clean[..^1]; scaled = 1_000_000m; }
        else if (clean.EndsWith("B", StringComparison.OrdinalIgnoreCase)) { clean = clean[..^1]; scaled = 1_000_000_000m; }
        else if (clean.EndsWith("T", StringComparison.OrdinalIgnoreCase)) { clean = clean[..^1]; scaled = 1_000_000_000_000m; }

        var match = NumberPattern.Match(clean.Replace(",", ""));
        if (!match.Success) return null;

        return decimal.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
            ? v * scaled
            : null;
    }
}
