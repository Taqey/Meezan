using System.Text.RegularExpressions;

namespace Meezan.Application.Common;

/// <summary>
/// Detects whether a Mubasher stock page has stale market data based on its SourceLastUpdateText.
/// Stale stocks on Mubasher display a DATE (e.g. Arabic month name or 4-digit year) instead of just a time today.
/// </summary>
public static partial class MubasherStaleDetector
{
    [GeneratedRegex(@"\b(19|20)\d{2}\b", RegexOptions.Compiled)]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"(يناير|فبراير|مارس|أبريل|ابريل|مايو|يونيو|يوليو|أغسطس|اغسطس|سبتمبر|أكتوبر|اكتوبر|نوفمبر|ديسمبر)", RegexOptions.Compiled)]
    private static partial Regex ArabicMonthRegex();

    /// <summary>
    /// Returns true if the last-update text indicates a stale/historical date rather than today's time.
    /// </summary>
    public static bool IsStale(string? sourceLastUpdateText)
    {
        if (string.IsNullOrWhiteSpace(sourceLastUpdateText))
            return false;

        // Check if text contains a 4-digit year or an Arabic month name
        if (YearRegex().IsMatch(sourceLastUpdateText))
            return true;

        if (ArabicMonthRegex().IsMatch(sourceLastUpdateText))
            return true;

        return false;
    }
}
