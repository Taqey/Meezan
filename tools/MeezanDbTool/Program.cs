using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Services.Jobs;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Models.Caching;

namespace MeezanDbTool;

public class Program
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("Updating SectorDaily rows in Database with Thndr-style recalculation...");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(ConnStr));

        services.AddScoped<ISectorDailyService, SectorDailyService>();
        services.AddScoped<IIndexStaticCache, DummyIndexCache>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sectorService = scope.ServiceProvider.GetRequiredService<ISectorDailyService>();

        var results = await sectorService.RecalculateSectorsAsync(null, "ThndrRecalculateCommit");

        Console.WriteLine($"Recalculated and persisted {results.Count} sectors to database!");
        foreach (var r in results.OrderBy(x => x.SectorId))
        {
            Console.WriteLine($"Sector {r.SectorId,-3} | Date: {r.TradingDate:yyyy-MM-dd} | ChangePct: {r.ChangePct:+0.00;-0.00}% | MarketCap: {r.TotalMarketCap:N0}");
        }
    }

    private class DummyIndexCache : IIndexStaticCache
    {
        public Task InvalidateAsync(string? indexCode = null, System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task<IndexStaticEntry?> GetAsync(string indexCode, System.Threading.CancellationToken ct = default) => Task.FromResult<IndexStaticEntry?>(null);
    }
}
