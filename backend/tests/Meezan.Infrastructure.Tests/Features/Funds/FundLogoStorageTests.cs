using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Meezan.Infrastructure.Services.Funds.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Meezan.Infrastructure.Tests.Features.Funds;

/// <summary>
/// The logo downloader writes to disk, so its safety rules are asserted directly:
/// sanitised filenames, rejection of non-image payloads, and rejection of
/// disallowed content types. Uses a loopback handler — no network.
/// </summary>
public class FundLogoStorageTests : IDisposable
{
    private readonly string _root = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "fund-logo-tests-" + Guid.NewGuid().ToString("N"));

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly byte[] _body;
        private readonly string? _contentType;
        public StubHandler(byte[] body, string? contentType) { _body = body; _contentType = contentType; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(_body) };
            if (_contentType != null) msg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(_contentType);
            return Task.FromResult(msg);
        }
    }

    private static readonly byte[] PngBytes =
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly byte[] JpegBytes =
        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x16, 0x4A, 0x46, 0x49, 0x46, 0, 0, 0, 0, 0, 0 };

    private FundLogoStorage Create(byte[] body, string? contentType, int maxBytes = 1024 * 1024)
    {
        var http = new HttpClient(new StubHandler(body, contentType));
        var opts = Options.Create(new FundLogoStorageOptions { RootPath = _root + System.IO.Path.DirectorySeparatorChar, MaxBytes = maxBytes });
        return new FundLogoStorage(http, opts, NullLogger<FundLogoStorage>.Instance);
    }

    [Fact]
    public async Task PngPayload_IsStored_WithDetectedExtension()
    {
        var storage = Create(PngBytes, "image/png");
        var path = await storage.StoreAsync("https://snduk.com/x/123-abc.png", "My Fund_1");

        Assert.NotNull(path);
        Assert.Equal("funds/my-fund-1.png", path);
        Assert.True(File.Exists(System.IO.Path.Combine(_root, "funds", "my-fund-1.png")));
    }

    [Fact]
    public async Task JpegPayload_ServedFromPngUrl_IsStoredAsJpg()
    {
        // SNDUK really does serve JPEG bytes from ".png" URLs — the stored
        // extension must follow the bytes, not the URL.
        var storage = Create(JpegBytes, "image/jpeg");
        var path = await storage.StoreAsync("https://snduk.com/x/123.png", "fund-x");

        Assert.NotNull(path);
        Assert.Equal("funds/fund-x.jpg", path);
        Assert.True(File.Exists(System.IO.Path.Combine(_root, "funds", "fund-x.jpg")));
    }

    [Fact]
    public async Task HtmlErrorPage_IsRejected_AndNothingWritten()
    {
        var html = System.Text.Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body>404 not found</body></html>");
        var storage = Create(html, "text/html");

        Assert.Null(await storage.StoreAsync("https://snduk.com/x/logo.png", "evil"));
        Assert.False(Directory.Exists(System.IO.Path.Combine(_root, "funds"))
            && Directory.GetFiles(System.IO.Path.Combine(_root, "funds")).Length > 0);
    }

    [Fact]
    public async Task DisallowedContentType_IsRejected()
    {
        var storage = Create(PngBytes, "application/x-msdownload");
        Assert.Null(await storage.StoreAsync("https://snduk.com/x/logo.png", "bad-type"));
    }

    [Fact]
    public async Task OversizePayload_IsRejected()
    {
        var storage = Create(PngBytes, "image/png", maxBytes: 4);
        Assert.Null(await storage.StoreAsync("https://snduk.com/x/logo.png", "too-big"));
    }

    [Theory]
    [InlineData("../../etc/passwd", "etcpasswd")]
    [InlineData("a/b/c", "abc")]
    [InlineData("  spaces  here ", "spaces-here")]
    [InlineData("UPPER_Case.1", "upper-case-1")]
    [InlineData("", "")]
    public void Sanitize_StripsPathSeparators(string input, string expected)
    {
        Assert.Equal(expected, FundLogoStorage.Sanitize(input));
    }

    [Fact]
    public void Sanitize_NeverReturnsTraversalSequences()
    {
        var outp = FundLogoStorage.Sanitize("../../../../windows/system32");
        Assert.DoesNotContain("..", outp);
        Assert.DoesNotContain("/", outp);
        Assert.DoesNotContain("\\", outp);
    }

    [Fact]
    public async Task TraversalInUrl_CannotEscapeRoot_BecauseFilenameComesFromSlug()
    {
        var storage = Create(PngBytes, "image/png");
        var path = await storage.StoreAsync("https://snduk.com/x/../../secret.png", "../../slug");

        // Uri normalisation removes the "..", and the stored name is derived from the
        // sanitised slug only — so nothing can be written outside <root>/funds.
        if (path != null)
        {
            var full = System.IO.Path.GetFullPath(System.IO.Path.Combine(_root, path));
            var allowed = System.IO.Path.GetFullPath(System.IO.Path.Combine(_root, "funds"));
            Assert.StartsWith(allowed, full, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("..", path);
        }
        else
        {
            // Rejecting outright is also acceptable.
            Assert.Null(path);
        }
    }

    [Fact]
    public async Task DisabledStorage_ReturnsNull_AndWritesNothing()
    {
        var http = new HttpClient(new StubHandler(PngBytes, "image/png"));
        var opts = Options.Create(new FundLogoStorageOptions { RootPath = "" });
        var storage = new FundLogoStorage(http, opts, NullLogger<FundLogoStorage>.Instance);

        Assert.Null(await storage.StoreAsync("https://snduk.com/x/logo.png", "slug"));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
    }
}
