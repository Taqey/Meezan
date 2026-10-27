using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Meezan.Infrastructure.Tests.Features.JobSettings;

/// <summary>
/// Pilot snapshot coverage for JobSettingsController (before CQRS migration).
/// Real HTTP routes, contract preservation: routes, [Authorize], JSON, status codes.
/// </summary>
public class JobSettingsRouteSnapshotTests
{
    private const string Key = "jobsettings-snapshot-test-key";

    private WebApplicationFactory<Program> CreateFactory()
    {
        var dbName = "jobsettings-snap-" + Guid.NewGuid().ToString("N");
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = Key,
                ["Funds:SchedulerEnabled"] = "false",
                ["Funds:Cache:WarmupOnStartup"] = "false",
                ["Redis:Host"] = ""
            }));
            b.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    (d.ServiceType.FullName ?? "").Contains("IDbContextOptionsConfiguration"))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);
                services.AddDbContext<ApplicationDbContext>(o =>
                {
                    o.UseInMemoryDatabase(dbName);
                    o.ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        });
        return factory;
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, bool withKey = true)
    {
        var client = factory.CreateClient();
        if (withKey) client.DefaultRequestHeaders.Add("X-Admin-Key", Key);
        return client;
    }

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        using var factory = CreateFactory();
        var anon = CreateClient(factory, withKey: false);
        var r = await anon.GetAsync("/api/job-settings");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task GetAll_Empty_Returns200_EmptyDict()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        var r = await client.GetAsync("/api/job-settings");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.EnumerateObject().Count());
        SnapshotAssert.MatchesStoredSnapshot("JobSettings_GetAll_Empty", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task Update_Unauthenticated_Returns401()
    {
        using var factory = CreateFactory();
        var anon = CreateClient(factory, withKey: false);
        var r = await anon.PutAsJsonAsync("/api/job-settings/LiveDaily", new { enabled = true });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Update_NewKey_CreatesAndReturns200_PersistedInGet()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        const string jobKey = "LiveDaily";

        var put = await client.PutAsJsonAsync($"/api/job-settings/{jobKey}", new { enabled = false });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var putBody = await put.Content.ReadAsStringAsync();
        using var putDoc = JsonDocument.Parse(putBody);
        Assert.Equal(jobKey, putDoc.RootElement.GetProperty("jobKey").GetString());
        Assert.False(putDoc.RootElement.GetProperty("enabled").GetBoolean());
        SnapshotAssert.MatchesStoredSnapshot("JobSettings_Put_LiveDaily_False", (int)put.StatusCode, putBody);

        var get = await client.GetAsync("/api/job-settings");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var getBody = await get.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getBody);
        Assert.True(getDoc.RootElement.TryGetProperty(jobKey, out var v));
        Assert.False(v.GetBoolean());
        SnapshotAssert.MatchesStoredSnapshot("JobSettings_GetAll_AfterPut", (int)get.StatusCode, getBody);
    }

    [Fact]
    public async Task Update_ExistingKey_TogglesValue()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        const string jobKey = "SlowQuarterly";

        var first = await client.PutAsJsonAsync($"/api/job-settings/{jobKey}", new { enabled = true });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PutAsJsonAsync($"/api/job-settings/{jobKey}", new { enabled = false });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(secondBody);
        Assert.Equal(jobKey, doc.RootElement.GetProperty("jobKey").GetString());
        Assert.False(doc.RootElement.GetProperty("enabled").GetBoolean());
        SnapshotAssert.MatchesStoredSnapshot("JobSettings_Put_Toggle_False", (int)second.StatusCode, secondBody);
    }
}
