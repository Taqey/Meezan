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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meezan.Infrastructure.Services;

public sealed class FundLogoStorageOptions
{
    /// <summary>Absolute or relative folder that production can serve. Empty = disabled.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>URL prefix used to build the public URL for a stored file.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public int MaxBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>Only these content types are written to disk.</summary>
    public string[] AllowedContentTypes { get; set; } =
        new[] { "image/png", "image/jpeg", "image/webp", "image/gif", "image/svg+xml" };

    /// <summary>Extensions allowed on the SOURCE url, before any content-type check.</summary>
    public string[] AllowedSourceExtensions { get; set; } =
        new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg" };
}

/// <summary>
/// Downloads SNDUK logos to a server-served folder so production does not depend on
/// Supabase reachability. Filenames are derived from the slug (never from the
/// remote URL) and the response must both pass an extension allow-list AND sniff
/// as an image, so an HTML error page can never be written as a .png.
/// </summary>
public sealed class FundLogoStorage : IFundLogoStorage
{
    private readonly HttpClient _http;
    private readonly FundLogoStorageOptions _options;
    private readonly ILogger<FundLogoStorage> _logger;

    public FundLogoStorage(HttpClient http, IOptions<FundLogoStorageOptions> options, ILogger<FundLogoStorage> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<string?> StoreAsync(string sourceUrl, string fundSlug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.RootPath)) return null; // disabled

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            return null;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return null;

        if (uri.AbsolutePath.Contains("..")) return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20));

            using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Fund logo {Url} returned HTTP {Status}.", sourceUrl, (int)response.StatusCode);
                return null;
            }

            var declaredType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (declaredType != null && !_options.AllowedContentTypes.Contains(declaredType))
            {
                _logger.LogWarning("Rejecting fund logo {Url}: content-type {Ct} not allowed.",
                    sourceUrl, declaredType);
                return null;
            }

            var bytes = await ReadBoundedAsync(response, _options.MaxBytes, cts.Token);
            if (bytes == null)
            {
                _logger.LogWarning("Fund logo {Url} exceeded the {Max} byte limit or failed the read.",
                    sourceUrl, _options.MaxBytes);
                return null;
            }

            // Safe, deterministic filename derived from the slug — never from the remote path.
            var safeName = Sanitize(fundSlug);
            if (string.IsNullOrWhiteSpace(safeName)) return null;

            // Sniff the magic bytes: never trust the declared content-type or the URL
            // extension — SNDUK serves JPEG data from ".png" URLs.
            var detected = DetectImage(bytes);
            if (detected == null)
            {
                _logger.LogWarning("Rejecting fund logo {Url}: payload is not a recognised image.", sourceUrl);
                return null;
            }

            var fileName = safeName + detected.Value.extension;
            var relativePath = "funds/" + fileName;

            var dir = Path.Combine(_options.RootPath, "funds");
            Directory.CreateDirectory(dir);
            var targetPath = Path.Combine(dir, fileName);

            await File.WriteAllBytesAsync(targetPath, bytes, cts.Token);
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store fund logo from {Url}.", sourceUrl);
            return null;
        }
    }

    private static async Task<byte[]?> ReadBoundedAsync(HttpResponseMessage response, int maxBytes, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > maxBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    /// <summary>
    /// Identifies the real format from the magic bytes and returns the extension
    /// the file must be stored with. Returns null for anything that is not a
    /// supported image, so an HTML error page can never be written as a logo.
    /// </summary>
    private static (string extension, string contentType)? DetectImage(byte[] b)
    {
        if (b.Length < 12) return null;
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return (".png", "image/png");
        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return (".jpg", "image/jpeg");
        if (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return (".gif", "image/gif");
        if (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return (".webp", "image/webp");
        return null;
    }

    /// <summary>Lowercase ascii slug; strips everything else so no traversal is possible.</summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input.Trim().ToLowerInvariant())
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-')
                sb.Append(ch);
            else if (ch == '_' || ch == '.' || char.IsWhiteSpace(ch))
                sb.Append('-');
        }
        var s = sb.ToString();
        while (s.Contains("--")) s = s.Replace("--", "-");
        return s.Trim('-');
    }
}