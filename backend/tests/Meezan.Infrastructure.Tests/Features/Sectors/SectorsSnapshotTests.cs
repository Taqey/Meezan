using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Meezan.Infrastructure.Tests.Features.Sectors;

public class SectorsSnapshotTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SectorsSnapshotTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!db.Sectors.Any(s => s.NameAr == "بنوك"))
        {
            var sector = new Sector
            {
                NameAr = "بنوك",
                NameEn = "Banks"
            };
            db.Sectors.Add(sector);

            var stock = new Stock
            {
                Ticker = "COMI",
                NameAr = "البنك التجاري الدولي",
                NameEn = "Commercial International Bank",
                Sector = sector,
                IsActive = true
            };
            db.Stocks.Add(stock);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetAll_Returns200_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/sectors");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Sectors_GetAll_Success", (int)response.StatusCode, content);
    }
}
