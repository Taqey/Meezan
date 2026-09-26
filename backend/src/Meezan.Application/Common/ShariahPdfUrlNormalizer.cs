namespace Meezan.Application.Common;

/// <summary>
/// Faisal Bank / Osoul compliance PDFs live on the Shariah data host
/// (stocks.templatesnippet.com), but the merged feed stores them as site-relative
/// paths ("/pdfs/..."). Bound directly, a relative path resolves against the
/// Meezan frontend origin and lands on a link-list/index page instead of the PDF.
/// Always normalise to the absolute direct-download URL.
/// </summary>
public static class ShariahPdfUrlNormalizer
{
    public const string SourceBaseUrl = "https://stocks.templatesnippet.com";

    public static string? Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        var u = url.Trim();
        if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return u;
        return SourceBaseUrl + "/" + u.TrimStart('/');
    }
}
