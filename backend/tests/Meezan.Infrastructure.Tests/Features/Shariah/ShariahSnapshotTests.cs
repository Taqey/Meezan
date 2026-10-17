using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;
using Meezan.Application.Features.Shariah.DTOs;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.Shariah;

public class ShariahSnapshotTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    public class FakeShariahSourceClient : IShariahSourceClient
    {
        public Task<List<ExternalStockMergedDto>> FetchMergedStocksAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<ExternalStockMergedDto>
            {
                new()
                {
                    Symbol = "CIB",
                    NameAr = "البنك التجاري الدولي",
                    NameEn = "Commercial International Bank",
                    CoreActivityCompliant = true,
                    LoansPercentage = 10m,
                    FaisalBank = new ExternalSourceOpinionDto
                    {
                        Status = "Compliant",
                        Percentage = 100m,
                        Note = null,
                        PdfUrl = null
                    }
                }
            };
            return Task.FromResult(list);
        }
    }

    public ShariahSnapshotTests()
    {
        _dbName = "shariah-snap-" + Guid.NewGuid().ToString("N");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Funds:SchedulerEnabled"] = "false"
            }));
            b.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    (d.ServiceType.FullName ?? "").Contains("IDbContextOptionsConfiguration") ||
                    d.ServiceType == typeof(IShariahSourceClient))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(o =>
                {
                    o.UseInMemoryDatabase(_dbName);
                    o.ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                services.AddScoped<IShariahSourceClient, FakeShariahSourceClient>();
            });
        });
    }

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var stock = new Stock
        {
            Ticker = "CIB",
            NameAr = "البنك التجاري الدولي",
            NameEn = "Commercial International Bank",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Stocks.Add(stock);
        await _db.SaveChangesAsync();

        var opinion = new ShariahSourceOpinion
        {
            StockId = stock.Id,
            SourceKey = ShariahSourceKey.FaisalBank,
            Status = "Compliant",
            Percentage = 100m,
            PdfUrl = "/uploads/shariah/Faisal compliance-list.pdf",
            FetchedAt = DateTime.UtcNow
        };
        _db.ShariahSourceOpinions.Add(opinion);
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Seed_WithPayload_Returns200()
    {
        var client = _factory.CreateClient();
        var payload = new Dictionary<string, ShariahSeedItemDto>
        {
            ["CIB"] = new()
            {
                Status = "Compliant",
                Pct = 100m,
                Note = "Initial seed test"
            }
        };

        var res = await client.PostAsJsonAsync("/api/shariah/seed", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_Seed_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task Refresh_Returns200()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync("/api/shariah/refresh", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_Refresh_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task RecomputeVerdicts_Returns200()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync("/api/shariah/recompute-verdicts", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_RecomputeVerdicts_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ImportFaisalOsoul_FileNotFound_Returns400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync("/api/shariah/import-faisal-osoul?path=nonexistent_file.json", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_ImportFaisalOsoul_FileNotFound", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetSourcePdfsStatus_Returns200()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/shariah/source-pdfs/status");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_GetSourcePdfsStatus_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetSourcePdf_InvalidSource_Returns404()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/shariah/source-pdf/UnknownSource");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_GetSourcePdf_InvalidSource", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetSourcePdf_ValidSource_Returns200Or404()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/shariah/source-pdf/FaisalBank");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_GetSourcePdf_FaisalBank", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task UploadSourceReportFile_InvalidSource_Returns400()
    {
        var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4 test"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "test.pdf");

        var res = await client.PostAsync("/api/shariah/sources/InvalidSource/report-file", form);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_UploadReportFile_InvalidSource", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task UploadSourceReportFile_NoFile_Returns400()
    {
        var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        var res = await client.PostAsync("/api/shariah/sources/FaisalBank/report-file", form);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_UploadReportFile_NoFile", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task UploadSourceReportFile_Success_Returns200()
    {
        var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4 sample content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "test.pdf");
        form.Add(new StringContent("2026-01-01"), "reportDate");

        var res = await client.PostAsync("/api/shariah/sources/FaisalBank/report-file", form);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Shariah_UploadReportFile_Success", (int)res.StatusCode, content);
    }
}
