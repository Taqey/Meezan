using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Meezan.Infrastructure.Persistence;

namespace MeezanDbTool;

public static class RenameAnfiToTycn
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task RunAsync()
    {
        Console.WriteLine("=== RENAMING ANFI TO (TYCN) تايكون انفستمنتس هولدنج ===");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(ConnStr);

        await using var db = new ApplicationDbContext(optionsBuilder.Options);

        var stock = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Ticker == "ANFI");

        if (stock == null)
        {
            Console.WriteLine("Stock ANFI not found in DB!");
            return;
        }

        Console.WriteLine($"Current Stock: Id={stock.Id}, Ticker={stock.Ticker}, NameAr={stock.NameAr}, NameEn={stock.NameEn}");

        // Update ticker and names
        stock.Ticker = "TYCN";
        stock.NameAr = "تايكون انفستمنتس هولدنج";
        stock.NameEn = "Tycoon Holding Company For Financial Investments";
        stock.UpdatedAt = DateTime.UtcNow;

        // If sector was null, assign to Non-bank financial services
        if (stock.SectorId == null)
        {
            var sector = await db.Sectors.FirstOrDefaultAsync(s => s.NameAr.Contains("مالية غير مصرفية") || s.NameAr == "الخدمات المالية");
            if (sector != null)
            {
                stock.SectorId = sector.Id;
                Console.WriteLine($"Assigned Sector: {sector.Id} ({sector.NameAr})");
            }
        }

        // If shares was missing or 0, set from stocks_to_fix (52,250,365)
        if (stock.MarketData != null)
        {
            if (stock.MarketData.SharesOutstanding == null || stock.MarketData.SharesOutstanding == 0)
            {
                stock.MarketData.SharesOutstanding = 52250365m;
                stock.MarketData.IsSharesManual = true;
                if (stock.MarketData.ClosingPrice.HasValue)
                {
                    stock.MarketData.MarketValue = stock.MarketData.ClosingPrice.Value * 52250365m;
                }
                Console.WriteLine($"Set SharesOutstanding: {stock.MarketData.SharesOutstanding:N0}, MarketValue: {stock.MarketData.MarketValue:N0}");
            }
        }

        // Update any StockDailySnapshots ticker for this stock
        var snaps = await db.StockDailySnapshots.Where(s => s.StockId == stock.Id).ToListAsync();
        foreach (var snap in snaps)
        {
            snap.Ticker = "TYCN";
            if (stock.SectorId.HasValue) snap.SectorId = stock.SectorId;
        }

        await db.SaveChangesAsync();
        Console.WriteLine("SUCCESS: ANFI successfully renamed to TYCN (تايكون انفستمنتس هولدنج)!");
    }
}

