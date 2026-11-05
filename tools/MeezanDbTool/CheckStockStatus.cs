using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Meezan.Infrastructure.Persistence;

namespace MeezanDbTool;

public static class CheckStockStatus
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task RunAsync()
    {
        Console.WriteLine("=== CHECKING STOCK STATUS FOR GOUR AND OTHER STOCKS ===");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(ConnStr);

        await using var db = new ApplicationDbContext(optionsBuilder.Options);

        var gour = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Ticker == "GOUR");

        if (gour != null)
        {
            Console.WriteLine($"GOUR: Id={gour.Id}, IsActive={gour.IsActive}, DataStatus={gour.DataStatus}, DeactivatedAt={gour.DeactivatedAt}, DeactivationReason={gour.DeactivationReason}, SectorId={gour.SectorId} ({gour.Sector?.NameAr})");
            Console.WriteLine($"  MD: Price={gour.MarketData?.ClosingPrice}, Shares={gour.MarketData?.SharesOutstanding}, MV={gour.MarketData?.MarketValue}");
            Console.WriteLine($"  SR: LastPrice={gour.SupportResistance?.LastPrice}, ChangePct={gour.SupportResistance?.ChangePct}");
        }
        else
        {
            Console.WriteLine("GOUR not found in DB!");
        }

        var inactiveStocks = await db.Stocks
            .Where(s => !s.IsActive || s.DataStatus != Meezan.Domain.Enums.StockDataStatus.Active)
            .Select(s => new { s.Ticker, s.IsActive, s.DataStatus, s.DeactivationReason })
            .ToListAsync();

        Console.WriteLine($"Total inactive or non-active DataStatus stocks: {inactiveStocks.Count}");
        foreach (var s in inactiveStocks)
        {
            Console.WriteLine($"  {s.Ticker,-10} | IsActive={s.IsActive} | DataStatus={s.DataStatus} | Reason={s.DeactivationReason}");
        }
    }
}
