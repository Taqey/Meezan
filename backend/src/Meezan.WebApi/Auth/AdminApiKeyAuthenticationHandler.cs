using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meezan.WebApi.Auth;

/// <summary>
/// Minimal shared-secret scheme for the admin surface.
/// Reads the secret from configuration (Admin:ApiKey) and expects it either as
/// "X-Admin-Key" or a Bearer token. Deliberately simple: the admin UI is a
/// static SPA, so there is no user store to integrate with.
/// </summary>
public class AdminApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "AdminApiKey";
    private const string HeaderName = "X-Admin-Key";

    private readonly IConfiguration _configuration;

    public AdminApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = _configuration["Admin:ApiKey"];

        if (string.IsNullOrWhiteSpace(expected))
        {
            // Fail closed: no configured secret means no admin access at all.
            Logger.LogError("Admin:ApiKey is not configured — all admin requests are rejected.");
            return Task.FromResult(AuthenticateResult.Fail("Admin API key not configured."));
        }

        var provided = Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(provided))
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                provided = authHeader["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Fixed-time comparison to avoid leaking the secret length/content by timing.
        var a = Encoding.UTF8.GetBytes(provided);
        var b = Encoding.UTF8.GetBytes(expected);
        var ok = a.Length == b.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);

        if (!ok)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid admin API key."));
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Admin") },
            SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}