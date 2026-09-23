using AngleSharp;
using AngleSharp.Html.Dom;
using Meezan.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Meezan.Infrastructure.Services;

/// <summary>
/// Scrapes mubasher.info for market data and support/resistance levels.
/// Faithful port of scraper.py and scrape_support_resistance.py.
/// </summary>
public partial class MubasherScraperClient : IStockScraperClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MubasherScraperClient> _logger;

    // Matches scraper.py: BASE_URL = "https://www.mubasher.info"
    private const string MarketDataUrlTemplate = "https://www.mubasher.info/markets/EGX/stocks/{symbol}";

    // Matches scrape_support_resistance.py: BASE_URL = "https://english.mubasher.info/markets/EGX/stocks/{symbol}/support-resistance"
    private const string SupportResistanceUrlTemplate = "https://english.mubasher.info/markets/EGX/stocks/{symbol}/support-resistance";

    // Matches scraper.py HEADERS
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    private const string ArabicAcceptLanguage = "ar,en;q=0.9";
    private const string EnglishAcceptLanguage = "en-US,en;q=0.9";

    // Arabic fields dictionary matching scraper.py FIELDS
    private static readonly Dictionary<string, string> MarketFields = new()
    {
        { "القيمة الاسمية", "nominal_value" },
        { "القيمة السوقية", "market_value" },
        { "القيمة الدفترية", "book_value" },
        { "مضاعف القيمة الدفترية", "pb_ratio" },
        { "ربحية السهم", "eps" },
        { "مضاعف الربحية", "pe_ratio" },
        { "عملة التداول", "currency" },
        { "أعلى", "high" },
        { "أدنى", "low" },
        { "فتح", "open" },
        { "إغلاق سابق", "previous_close" }  // fallback price when live price is 0
    };

    private const string LastUpdateLabel = "آخر تحديث";

    // Regexes from scraper.py
    private static readonly Regex PriceRegex = new(@"^[-+]?[\d,]*\.?\d+$", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"[-+]?[\d,]*\.?\d+", RegexOptions.Compiled);

    // Regexes from scrape_support_resistance.py
    private static readonly Regex SrLastPriceRegex = new(@"\n?\s*([\d,]+[.,]?\d*)\s*\n", RegexOptions.Compiled);
    private static readonly Regex SrVolumeRegex = new(@"Volume\s*\n?\s*([\d,]+)", RegexOptions.Compiled);
    private static readonly Regex SrPctToPivotRegex = new(@"Price to pivot point\s*\n?\s*(-?[\d,]+[.,]?\d*)\s*%", RegexOptions.Compiled);
    private static readonly Regex SrR2Regex = new(@"Second resistance level\s*\(r2\)\s*(-?[\d,]+[.,]?\d*)", RegexOptions.Compiled);
    private static readonly Regex SrR1Regex = new(@"First resistance level\s*\(r1\)\s*(-?[\d,]+[.,]?\d*)", RegexOptions.Compiled);
    private static readonly Regex SrPivotRegex = new(@"Pivot point\s*(-?[\d,]+[.,]?\d*)", RegexOptions.Compiled);
    private static readonly Regex SrSupportRegex = new(@"(?:First|Second) support level\s*\(d1\)\s*(-?[\d,]+[.,]?\d*)", RegexOptions.Compiled);
    private static readonly Regex SrHeaderChangeRegex = new(@"\n(-?[\d,]+[.,]?\d*)\n(-?[\d,]+[.,]?\d*)\s*%", RegexOptions.Compiled);

    public MubasherScraperClient(HttpClient httpClient, ILogger<MubasherScraperClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Scrapes market data for one stock.
    /// Faithfully ports scraper.py scrape_stock(session, stock, retries=2).
    /// </summary>
    public async Task<ScrapedMarketData?> ScrapeMarketDataAsync(string ticker, CancellationToken ct = default)
    {
        var url = MarketDataUrlTemplate.Replace("{symbol}", ticker.ToUpperInvariant());
        
        // Retries = 2 (meaning 3 total attempts: attempt in range(retries + 1))
        // Delay on retry = 1.5s (matching scraper.py time.sleep(1.5))
        const int retries = 2;

        for (int attempt = 0; attempt <= retries; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", UserAgent);
                request.Headers.Add("Accept-Language", ArabicAcceptLanguage);

                using var response = await _httpClient.SendAsync(request, ct);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Mubasher returned 404 Not Found for {Ticker} ({Url}).", ticker, url);
                    return new ScrapedMarketData(ticker, null, null, null, null, null, null, null, null, null, null, null, null, IsNotFound: true);
                }

                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync(ct);
                return await ParseMarketDataHtmlAsync(ticker, html, ct);
            }
            catch (Exception ex)
            {
                if (attempt == retries)
                {
                    _logger.LogWarning("Failed scraping market data for {Ticker} ({Url}) after {Attempts} attempts: {Error}",
                        ticker, url, retries + 1, ex.Message);
                    return null;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
            }
        }

        return null;
    }

    /// <summary>
    /// Scrapes support/resistance levels for one stock.
    /// Faithfully ports scrape_support_resistance.py fetch_stock_data(symbol, retries=3, delay=1.5).
    /// </summary>
    public async Task<ScrapedSupportResistance?> ScrapeSupportResistanceAsync(string ticker, CancellationToken ct = default)
    {
        var url = SupportResistanceUrlTemplate.Replace("{symbol}", ticker.ToUpperInvariant());

        // Retries = 3 (attempt in range(retries)), delay = 1.5s
        const int retries = 3;
        string? lastError = null;

        for (int attempt = 0; attempt < retries; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", UserAgent);
                request.Headers.Add("Accept-Language", EnglishAcceptLanguage);

                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)response.StatusCode}";
                    await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
                    continue;
                }

                var html = await response.Content.ReadAsStringAsync(ct);
                var (data, error) = await ParseSupportResistanceHtmlAsync(ticker, html, ct);

                if (data is not null)
                {
                    return data;
                }

                lastError = error;
                await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
            }
            catch (Exception ex)
            {
                lastError = $"Exception: {ex.Message}";
                await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
            }
        }

        _logger.LogWarning("Failed scraping S/R for {Ticker} ({Url}): {Error}", ticker, url, lastError);
        return null;
    }

    // ── Market Data Parsing (scraper.py logic) ────────────────────────────────

    private static async Task<ScrapedMarketData?> ParseMarketDataHtmlAsync(string ticker, string html, CancellationToken ct)
    {
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(req => req.Content(html), ct) as IHtmlDocument;
        if (doc is null) return null;

        // scraper.py:
        // full_text = soup.get_text(separator="|", strip=True)
        // segments = [s for s in full_text.split("|") if s.strip()]
        var segments = ExtractTextSegments(doc, '|');

        var fieldValues = new Dictionary<string, decimal?>();
        string? currency = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i].Trim();
            foreach (var (label, key) in MarketFields)
            {
                if (seg == label)
                {
                    if (i + 1 < segments.Count)
                    {
                        var rawValue = segments[i + 1].Trim();
                        if (key == "currency")
                        {
                            currency = rawValue;
                        }
                        else
                        {
                            var (num, _) = SplitValue(rawValue);
                            fieldValues[key] = num;
                        }
                    }
                }
            }
        }

        // scraper.py extract_price_and_update(html)
        var (closingPrice, lastUpdateText) = ExtractPriceAndUpdate(segments);

        // Fallback: if the live price is missing or zero (e.g. low-volume / suspended stocks
        // that Mubasher displays as 0.00), use "إغلاق سابق" (previous close) instead.
        var previousClose = fieldValues.GetValueOrDefault("previous_close");
        if ((closingPrice is null || closingPrice == 0m) && previousClose is > 0m)
        {
            closingPrice = previousClose;
        }

        return new ScrapedMarketData(
            Ticker: ticker,
            NominalValue: fieldValues.GetValueOrDefault("nominal_value"),
            MarketValue: fieldValues.GetValueOrDefault("market_value"),
            BookValue: fieldValues.GetValueOrDefault("book_value"),
            PbRatio: fieldValues.GetValueOrDefault("pb_ratio"),
            Eps: fieldValues.GetValueOrDefault("eps"),
            PeRatio: fieldValues.GetValueOrDefault("pe_ratio"),
            Currency: currency,
            High: fieldValues.GetValueOrDefault("high"),
            Low: fieldValues.GetValueOrDefault("low"),
            Open: fieldValues.GetValueOrDefault("open"),
            ClosingPrice: closingPrice,
            SourceLastUpdateText: lastUpdateText
        );
    }

    /// <summary>
    /// Faithful port of scraper.py split_value(raw):
    /// Extracts the numeric portion and extra text.
    /// Example: "9.75 جنيه مصري بناءً على: الربع الثانى 2025" -> number=9.75
    /// </summary>
    private static (decimal? Number, string Extra) SplitValue(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (null, string.Empty);

        var match = NumberRegex.Match(raw);
        if (!match.Success)
            return (null, raw.Trim());

        var numStr = match.Value.Replace(",", "");
        decimal? number = decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

        var extra = (raw[..match.Index] + raw[(match.Index + match.Length)..]).Trim();
        return (number, extra);
    }

    /// <summary>
    /// Faithful port of scraper.py extract_price_and_update(html):
    /// Searches for segment starting with "آخر تحديث", then checks next 3 segments for first PRICE_RE match.
    /// </summary>
    private static (decimal? Price, string? LastUpdateText) ExtractPriceAndUpdate(List<string> segments)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i].Trim();
            if (seg.StartsWith(LastUpdateLabel))
            {
                var lastUpdateText = seg;
                int maxLookahead = Math.Min(i + 4, segments.Count);
                for (int j = i + 1; j < maxLookahead; j++)
                {
                    var candidate = segments[j].Trim();
                    var candidateClean = candidate.Replace(",", "");
                    if (PriceRegex.IsMatch(candidateClean))
                    {
                        if (decimal.TryParse(candidateClean, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                        {
                            return (price, lastUpdateText);
                        }
                    }
                }
                return (null, lastUpdateText);
            }
        }

        return (null, null);
    }

    // ── Support / Resistance Parsing (scrape_support_resistance.py logic) ──────

    private static async Task<(ScrapedSupportResistance? Data, string? Error)> ParseSupportResistanceHtmlAsync(
        string ticker, string html, CancellationToken ct)
    {
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(req => req.Content(html), ct) as IHtmlDocument;
        if (doc is null) return (null, "Failed to parse HTML document");

        // raw_lines = [ln.strip() for ln in soup.get_text("\n").split("\n")]
        // lines = [ln for ln in raw_lines if ln]
        // text = "\n".join(lines)
        var lines = ExtractTextLines(doc);
        var text = string.Join("\n", lines);

        if (!text.Contains("Support and resistance"))
        {
            return (null, "Section 'Support and resistance' not found in page");
        }

        // text.rsplit("Support and resistance", 1)[1]
        int lastIndex = text.LastIndexOf("Support and resistance", StringComparison.Ordinal);
        var srText = text[(lastIndex + "Support and resistance".Length)..];

        // Raw extractions
        var lastPriceRaw = FindNum(SrLastPriceRegex, srText);
        var pctToPivotRaw = FindNum(SrPctToPivotRegex, srText);
        var r2Raw = FindNum(SrR2Regex, srText);
        var r1Raw = FindNum(SrR1Regex, srText);
        var pivotRaw = FindNum(SrPivotRegex, srText);

        // s_matches = re.findall(r"(?:First|Second) support level\s*\(d1\)\s*(-?[\d,]+[.,]?\d*)", sr_text)
        // Note: Mubasher HTML typo in English page uses (d1) for both First and Second support levels.
        var sMatches = SrSupportRegex.Matches(srText);
        string? s1Raw = sMatches.Count > 0 ? sMatches[0].Groups[1].Value : null;
        string? s2Raw = sMatches.Count > 1 ? sMatches[1].Groups[1].Value : null;

        // Header text for change_pct
        // header_text = text.split("Support and resistance", 1)[0]
        int firstIndex = text.IndexOf("Support and resistance", StringComparison.Ordinal);
        var headerText = text[..firstIndex];

        // change_match = re.search(r"\n(-?[\d,]+[.,]?\d*)\n(-?[\d,]+[.,]?\d*)\s*%", header_text)
        // change_pct_raw = change_match.group(2) if change_match else pct_to_pivot_raw
        var changeMatch = SrHeaderChangeRegex.Match(headerText);
        string? changePctRaw = changeMatch.Success ? changeMatch.Groups[2].Value : pctToPivotRaw;

        // Clean values via clean_number(raw)
        var lastPrice = CleanNumber(lastPriceRaw);
        var r2 = CleanNumber(r2Raw);
        var r1 = CleanNumber(r1Raw);
        var pivot = CleanNumber(pivotRaw);
        var s1 = CleanNumber(s1Raw);
        var s2 = CleanNumber(s2Raw);
        var changePct = CleanNumber(changePctRaw);

        // Verify required non-null fields matching scrape_support_resistance.py
        var missing = new List<string>();
        if (!lastPrice.HasValue) missing.Add("last_price");
        if (!r2.HasValue) missing.Add("r2");
        if (!r1.HasValue) missing.Add("r1");
        if (!pivot.HasValue) missing.Add("pivot");
        if (!s1.HasValue) missing.Add("s1");
        if (!s2.HasValue) missing.Add("s2");

        if (missing.Count > 0)
        {
            return (null, $"Missing or unconvertible values: {string.Join(", ", missing)}");
        }

        var result = new ScrapedSupportResistance(
            Ticker: ticker,
            LastPrice: lastPrice,
            ChangePct: changePct,
            Pivot: pivot,
            R1: r1,
            R2: r2,
            S1: s1,
            S2: s2
        );

        return (result, null);
    }

    /// <summary>
    /// Faithful port of scrape_support_resistance.py find_num(pattern, s):
    /// Returns group(1) or null.
    /// </summary>
    private static string? FindNum(Regex pattern, string text)
    {
        var match = pattern.Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Faithful port of scrape_support_resistance.py clean_number(raw):
    /// Cleans commas, removes non-digits except '.', '-', and parses to decimal.
    /// </summary>
    private static decimal? CleanNumber(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var s = raw.Trim();
        // Remove anything that is not digit, comma, dot, minus
        s = Regex.Replace(s, @"[^\d.,\-]", "");
        if (string.IsNullOrWhiteSpace(s)) return null;

        // Remove thousands commas
        s = s.Replace(",", "");

        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val;

        return null;
    }

    // ── Helper Text Extractors ───────────────────────────────────────────────

    private static List<string> ExtractTextSegments(IHtmlDocument doc, char separator)
    {
        var segments = new List<string>();
        if (doc.Body is null) return segments;

        WalkTextNodes(doc.Body, segments);
        return segments;
    }

    private static List<string> ExtractTextLines(IHtmlDocument doc)
    {
        var lines = new List<string>();
        if (doc.Body is null) return lines;

        WalkTextNodes(doc.Body, lines);
        return lines;
    }

    private static void WalkTextNodes(AngleSharp.Dom.INode node, List<string> output)
    {
        // Skip script and style tags matching BeautifulSoup get_text
        if (node is AngleSharp.Html.Dom.IHtmlScriptElement or AngleSharp.Html.Dom.IHtmlStyleElement)
            return;

        if (node.NodeType == AngleSharp.Dom.NodeType.Text)
        {
            var text = node.TextContent?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                output.Add(text);
            }
            return;
        }

        foreach (var child in node.ChildNodes)
        {
            WalkTextNodes(child, output);
        }
    }
}
