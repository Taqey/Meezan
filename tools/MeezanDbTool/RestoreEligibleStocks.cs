using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;

namespace MeezanDbTool;

public static class RestoreEligibleStocks
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task RunAsync()
    {
        Console.WriteLine("=== RESTORING ELIGIBLE STOCKS (INCLUDING GOUR) TO ACTIVE ===");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(ConnStr);

        await using var db = new ApplicationDbContext(optionsBuilder.Options);

        // Find stocks that have valid price & market data now (e.g. from Starta or fix list)
        // but were flagged IncompleteNoData or soft-deactivated
        var stocks = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Where(s => s.MarketData != null && s.MarketData.ClosingPrice.HasValue && s.MarketData.ClosingPrice.Value > 0)
            .ToListAsync();

        int restoredDataStatus = 0;
        int restoredIsActive = 0;

        foreach (var s in stocks)
        {
            bool modified = false;
            if (s.DataStatus != StockDataStatus.Active)
            {
                s.DataStatus = StockDataStatus.Active;
                s.UpdatedAt = DateTime.UtcNow;
                restoredDataStatus++;
                modified = true;
                Console.WriteLine($"Restored DataStatus to Active for {s.Ticker} (Price: {s.MarketData?.ClosingPrice})");
            }

            // If deactivated because of missing data / scrape failure in past, restore IsActive
            if (!s.IsActive)
            {
                s.IsActive = true;
                s.DataStatus = StockDataStatus.Active;
                s.DeactivatedAt = null;
                s.DeactivationReason = null;
                s.UpdatedAt = DateTime.UtcNow;
                restoredIsActive++;
                modified = true;
                Console.WriteLine($"Reactivated IsActive=true for {s.Ticker} (Price: {s.MarketData?.ClosingPrice})");
            }
        }

        await db.SaveChangesAsync();

        Console.WriteLine("=================================================");
        Console.WriteLine($"Restored DataStatus = Active: {restoredDataStatus} stocks");
        Console.WriteLine($"Restored IsActive = true: {restoredIsActive} stocks");
        Console.WriteLine("=================================================");

        // Verify GOUR specifically
        var gour = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Ticker == "GOUR");

        if (gour != null)
        {
            Console.WriteLine($"VERIFICATION: GOUR is now IsActive={gour.IsActive}, DataStatus={gour.DataStatus}, Price={gour.MarketData?.ClosingPrice}, Sector={gour.Sector?.NameAr}");
        }
    }
}
