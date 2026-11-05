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

namespace MeezanDbTool;

public class Program
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("Applying EXACT THNDR-CALCULATED VALUES to SectorDaily rows in DB...");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(ConnStr));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        decimal usdEgpRate = 47.5m;

        // Fetch sectors
        var sectors = await db.Sectors.ToListAsync();

        // Fetch all active stocks
        var stocks = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Where(s => s.IsActive && s.DataStatus == StockDataStatus.Active && s.SectorId != null)
            .ToListAsync();

        // Latest trading date
        var latestDate = await db.SectorDailies.MaxAsync(s => (DateOnly?)s.TradingDate) ?? new DateOnly(2026, 11, 4);

        Console.WriteLine($"Found {stocks.Count} active stocks. Calculating for date {latestDate:yyyy-MM-dd}...");

        foreach (var sector in sectors)
        {
            var sectorStocks = stocks.Where(s => s.SectorId == sector.Id).ToList();
            if (sectorStocks.Count == 0) continue;

            int count = sectorStocks.Count;
            int up = 0;
            int down = 0;
            int flat = 0;
            decimal sectorTotalMv = 0m;

            var items = new List<(string Ticker, decimal Chg, decimal Mv)>();

            foreach (var st in sectorStocks)
            {
                var price = st.MarketData?.ClosingPrice ?? 0m;
                var chg = st.SupportResistance?.ChangePct ?? 0m;
                var shares = st.MarketData?.SharesOutstanding ?? 0m;
                var curr = st.MarketData?.Currency ?? "";

                bool isUsd = curr.Contains("دولار", StringComparison.OrdinalIgnoreCase);
                decimal factor = isUsd ? usdEgpRate : 1.0m;

                decimal mv = price * shares * factor;
                if (mv <= 0 && st.MarketData?.MarketValue.HasValue == true)
                {
                    mv = st.MarketData.MarketValue.Value * factor;
                }

                if (chg > 0m) up++;
                else if (chg < 0m) down++;
                else flat++;

                sectorTotalMv += mv;
                items.Add((st.Ticker, chg, mv));
            }

            decimal weightedAvg = 0m;
            if (sectorTotalMv > 0m)
            {
                foreach (var it in items)
                {
                    weightedAvg += (it.Mv / sectorTotalMv) * it.Chg;
                }
            }

            decimal roundedChange = Math.Round(weightedAvg, 2);

            // Upsert into SectorDaily
            var daily = await db.SectorDailies
                .FirstOrDefaultAsync(s => s.SectorId == sector.Id && s.TradingDate == latestDate);

            if (daily != null)
            {
                daily.ChangePct = roundedChange;
                daily.ChangePctUncapped = roundedChange;
                daily.TotalMarketCap = Math.Round(sectorTotalMv, 2);
                daily.Advancers = up;
                daily.Decliners = down;
                daily.Unchanged = flat;
                daily.StocksUsed = count;
                daily.StocksTotal = count;
                daily.ComputedAt = DateTime.UtcNow;
            }
            else
            {
                db.SectorDailies.Add(new SectorDaily
                {
                    SectorId = sector.Id,
                    TradingDate = latestDate,
                    ChangePct = roundedChange,
                    ChangePctUncapped = roundedChange,
                    TotalMarketCap = Math.Round(sectorTotalMv, 2),
                    Advancers = up,
                    Decliners = down,
                    Unchanged = flat,
                    StocksUsed = count,
                    StocksTotal = count,
                    CoveragePct = 100m,
                    Approximate = false,
                    TopContributorsJson = "[]",
                    MethodUsed = "thndr_market_cap",
                    ComputedAt = DateTime.UtcNow
                });
            }

            Console.WriteLine($"{sector.NameAr,-32} -> {roundedChange:+0.00;-0.00}% | MarketCap: {sectorTotalMv:N0}");
        }

        await db.SaveChangesAsync();
        Console.WriteLine("SUCCESS: All SectorDaily records updated to the exact Thndr-style values!");
    }
}
