using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Services.Jobs;

namespace MeezanDbTool;

public static class RecalcSectors
{
    private const string ConnectionString = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task RunAsync()
    {
        Console.WriteLine("=== RECALCULATING SECTOR DAILY METRICS VIA SECTOR DAILY SERVICE ===");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(ConnectionString);

        await using var db = new ApplicationDbContext(optionsBuilder.Options);
        var logger = NullLogger<SectorDailyService>.Instance;
        var sectorService = new SectorDailyService(db, logger);

        var result = await sectorService.RecalculateSectorsAsync(null, "Starta live price migration");

        Console.WriteLine($"Calculated returns for {result.Count} sectors:");
        var sectors = await db.Sectors.ToListAsync();
        var sectorMap = sectors.ToDictionary(s => s.Id, s => s.NameAr);

        foreach (var r in result.OrderByDescending(r => r.ChangePct))
        {
            var name = sectorMap.GetValueOrDefault(r.SectorId, "Unknown");
            Console.WriteLine($"{name,-25} | Return: {(r.ChangePct >= 0 ? "+" : "")}{r.ChangePct:F2}% | Adv: {r.Advancers} | Dec: {r.Decliners} | Unch: {r.Unchanged} | MCap: {r.TotalMarketCap:N0}");
        }
    }
}
