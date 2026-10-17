using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Meezan.Infrastructure.Tests.Features.Indices;

public class IndicesSnapshotTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public IndicesSnapshotTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!db.Indices.Any(i => i.Code == "EGX30"))
        {
            var index = new Meezan.Domain.Entities.Index
            {
                Code = "EGX30",
                NameAr = "مؤشر إيجي إكس 30",
                NameEn = "EGX 30 Index",
                Description = "Main benchmark",
                LastUpdated = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc)
            };
            db.Indices.Add(index);

            var stock = new Stock
            {
                Ticker = "COMI",
                NameAr = "البنك التجاري الدولي",
                NameEn = "Commercial International Bank",
                IsActive = true
            };
            db.Stocks.Add(stock);
            await db.SaveChangesAsync();

            db.IndexConstituents.Add(new IndexConstituent
            {
                IndexId = index.Id,
                StockId = stock.Id,
                Weight = 30.5m
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetAll_Returns200_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/indices");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Indices_GetAll_Success", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task GetConstituents_Returns200_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/indices/EGX30/constituents");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Indices_GetConstituents_Success", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task GetConstituents_NotFound_Returns404_MatchesSnapshot()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/indices/NONEXISTENT/constituents");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Indices_GetConstituents_NotFound", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task UploadIndexFile_EmptyFile_Returns400_MatchesSnapshot()
    {
        var client = _factory.CreateClient();

        using var form = new MultipartFormDataContent();
        var emptyContent = new ByteArrayContent(Array.Empty<byte>());
        form.Add(emptyContent, "file", "empty.xlsx");

        var response = await client.PostAsync("/api/indices/EGX30/upload", form);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Indices_Upload_EmptyFile", (int)response.StatusCode, content);
    }
}
