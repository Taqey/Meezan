using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Meezan.Infrastructure.Persistence;

namespace MeezanDbTool;

public static class CheckSnapshots
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task RunAsync()
    {
        Console.WriteLine("=== CHECKING STOCK DAILY SNAPSHOTS VS STOCK SUPPORT RESISTANCE ===");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(ConnStr);

        await using var db = new ApplicationDbContext(optionsBuilder.Options);

        var snapDate = new DateOnly(2026, 11, 4);

        var stocks = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Where(s => s.Sector != null && s.Sector.NameAr.Contains("أساسية"))
            .ToListAsync();

        var snaps = await db.StockDailySnapshots
            .Where(s => s.TradingDate == snapDate && s.Sector != null && s.Sector.NameAr.Contains("أساسية"))
            .ToListAsync();

        var snapMap = snaps.ToDictionary(s => s.StockId);

        foreach (var s in stocks)
        {
            snapMap.TryGetValue(s.Id, out var snap);
            Console.WriteLine($"{s.Ticker,-10} | SR Chg: {s.SupportResistance?.ChangePct,6:F2}% | Snap Chg: {snap?.ChangePct,6:F2}% | Snap Price: {snap?.Price,8:F2} | MD Price: {s.MarketData?.ClosingPrice,8:F2}");
        }
    }
}

