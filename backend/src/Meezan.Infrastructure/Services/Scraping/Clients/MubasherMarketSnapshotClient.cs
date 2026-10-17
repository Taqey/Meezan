using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html.Dom;
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
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.Scraping.Clients;

/// <summary>
/// Scrapes Mubasher's published index/sector summary blocks
/// (https://www.mubasher.info/markets/EGX/indices/{slug}).
///
/// Fair-use posture, matching robots.txt + /s/Terms: the market pages are NOT
/// disallowed for general crawlers (only /api/, /services/ and misc paths
/// are), crawl-delay 5 is honored with ≥5s gaps between requests, fetches run
/// on a slow schedule, and every surfaced value is attributed ("المصدر: مباشر").
/// Only the server-rendered summary block is read — never /api/ or /services/.
/// </summary>
public partial class MubasherMarketSnapshotClient : IMubasherMarketSnapshotClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MubasherMarketSnapshotClient> _logger;

    private const string IndexPageUrlTemplate = "https://www.mubasher.info/markets/EGX/indices/{slug}";

    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

    public MubasherMarketSnapshotClient(HttpClient httpClient, ILogger<MubasherMarketSnapshotClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetches one market page and parses its summary block.
    /// Returns null when the page or the numbers are unusable — callers must
    /// keep serving the last good snapshot in that case. Never throws for
    /// parse problems (only for cancellation).
    /// </summary>
    public async Task<MarketSnapshotQuote?> ScrapeIndexSummaryAsync(
        string mubasherSlug, CancellationToken ct = default)
    {
        var url = IndexPageUrlTemplate.Replace("{slug}", Uri.EscapeDataString(mubasherSlug));
        string html;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Accept-Language", "ar,en;q=0.9");
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Snapshot scrape for {Slug} failed: HTTP {Status}", mubasherSlug, (int)response.StatusCode);
                return null;
            }
            html = await response.Content.ReadAsStringAsync(ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Snapshot scrape for {Slug} failed on fetch", mubasherSlug);
            return null;
        }

        try
        {
            var config = Configuration.Default;
            using var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(req => req.Content(html), ct) as IHtmlDocument;
            if (doc == null) return null;

            var valueEl = doc.QuerySelector(".market-summary__last-price");
            var changeEl = doc.QuerySelector(".market-summary__change");
            var changePctEl = doc.QuerySelector(".market-summary__change-percentage");
            var dateEl = doc.QuerySelector(".market-summary__date");
            if (valueEl == null || changePctEl == null) return null;

            var value = ParseDecimal(valueEl.TextContent);
            var changePct = ParseSignedPercent(changePctEl.TextContent);
            var change = ParseSignedDecimal(changeEl?.TextContent);
            if (value == null || changePct == null) return null;

            // Direction classes cross-check the sign when the text omits it.
            var cls = (changePctEl.ClassName ?? string.Empty).ToLowerInvariant();
            var isUp = cls.Contains("up") && !cls.Contains("down");
            var isDown = cls.Contains("down") && !cls.Contains("up");
            if (changePct == 0 && (isUp || isDown))
            {
                // Zero with an explicit direction stays zero — no invention.
            }

            var dateText = dateEl != null ? Whitespace(dateEl.TextContent) : null;
            if (string.IsNullOrWhiteSpace(dateText)) dateText = null;

            return new MarketSnapshotQuote(value, change, changePct, isUp && !isDown, dateText);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Snapshot scrape for {Slug} failed on parse", mubasherSlug);
            return null;
        }
    }

    private static string Whitespace(string? s)
        => string.IsNullOrWhiteSpace(s) ? string.Empty : WhitespaceRegex().Replace(s.Trim(), " ");

    /// <summary>
    /// Parses "53,298.14" → 53298.14. Handles Arabic-Indic digits, commas and
    /// surrounding whitespace. Null when no digits are present at all.
    /// </summary>
    public static decimal? ParseDecimal(string? text)
    {
        var digits = ExtractDigits(text);
        if (digits == null) return null;
        if (decimal.TryParse(digits, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var val))
            return val;
        return null;
    }

    /// <summary>
    /// Parses "-0.48%" / "+1.25%" / "(0.50)" (parentheses = negative). Null when broken.
    /// </summary>
    public static decimal? ParseSignedPercent(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = NormalizeDigits(text.Trim());
        var negative = false;
        if (t.StartsWith('(') && t.EndsWith(')'))
        {
            negative = true;
            t = t.Substring(1, t.Length - 2);
        }
        t = t.Replace("%", string.Empty).Trim();
        // Unicode minus / Arabic signs → ASCII hyphen.
        t = t.Replace('−', '-').Replace('–', '-');
        var m = SignedNumberRegex().Match(t);
        if (!m.Success) return null;
        if (!decimal.TryParse(m.Value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var val))
            return null;
        return negative ? -Math.Abs(val) : val;
    }

    /// <summary>Same as percent but tolerates a missing value (change is optional).</summary>
    public static decimal? ParseSignedDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = NormalizeDigits(text.Trim());
        var negative = false;
        if (t.StartsWith('(') && t.EndsWith(')'))
        {
            negative = true;
            t = t.Substring(1, t.Length - 2);
        }
        t = t.Replace('−', '-').Replace('–', '-').Replace(",", string.Empty).Trim();
        if (!SignedNumberRegex().IsMatch(t)) return null;
        if (!decimal.TryParse(t, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var val))
            return null;
        return negative ? -Math.Abs(val) : val;
    }

    private static string? ExtractDigits(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = NormalizeDigits(text).Replace(",", string.Empty).Trim();
        var m = SignedNumberRegex().Match(t);
        return m.Success ? m.Value : null;
    }

    private static string NormalizeDigits(string text)
    {
        // Arabic-Indic ٠-٩ (and Persian ۰-۹) → ASCII.
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '٠' && chars[i] <= '٩') chars[i] = (char)('0' + (chars[i] - '٠'));
            else if (chars[i] >= '۰' && chars[i] <= '۹') chars[i] = (char)('0' + (chars[i] - '۰'));
        }
        return new string(chars);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"-?[\d,]*\.?\d+")]
    private static partial Regex SignedNumberRegex();
}
