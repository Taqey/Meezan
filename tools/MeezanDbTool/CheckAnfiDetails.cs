using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Meezan.Infrastructure.Persistence;

namespace MeezanDbTool;

public static class CheckAnfiDetails
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task RunAsync()
    {
        Console.WriteLine("=== CHECKING ANFI DETAILS ===");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(ConnStr);

        await using var db = new ApplicationDbContext(optionsBuilder.Options);

        var anfi = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Ticker == "ANFI");

        if (anfi != null)
        {
            Console.WriteLine($"Found ANFI: Id={anfi.Id}, Ticker={anfi.Ticker}, NameAr={anfi.NameAr}, NameEn={anfi.NameEn}, Sector={anfi.Sector?.NameAr}, Price={anfi.MarketData?.ClosingPrice}");
        }
        else
        {
            Console.WriteLine("ANFI not found in DB!");
        }

        var tycn = await db.Stocks.FirstOrDefaultAsync(s => s.Ticker == "TYCN");
        if (tycn != null)
        {
            Console.WriteLine($"Found TYCN: Id={tycn.Id}, Ticker={tycn.Ticker}, NameAr={tycn.NameAr}, NameEn={tycn.NameEn}");
        }
    }
}

