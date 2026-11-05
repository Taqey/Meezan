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
        Console.WriteLine("=== THNDR-STYLE SECTOR CALCULATION ===");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(ConnStr));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Usd Rate
        decimal usdEgpRate = 47.5m; // Thndr / official market rate

        // Fetch all active sectors
        var sectors = await db.Sectors.AsNoTracking().OrderBy(s => s.Id).ToListAsync();

        // Fetch all active stocks with MarketData and SupportResistance
        var stocks = await db.Stocks
            .AsNoTracking()
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Where(s => s.IsActive && s.DataStatus == StockDataStatus.Active && s.SectorId != null)
            .ToListAsync();

        Console.WriteLine($"Total Active Stocks with Sector: {stocks.Count}");

        Console.WriteLine();
        Console.WriteLine($"{"Sector Name (Ar)",-32} | {"Total Stocks",-12} | {"Up",-4} | {"Down",-4} | {"Flat",-4} | {"Simple Avg %",-14} | {"Thndr W. Avg %",-16} | {"Market Cap (M EGP)",-20}");
        Console.WriteLine(new string('-', 120));

        decimal grandTotalMv = 0m;
        int grandTotalStocks = 0;
        int grandTotalUp = 0;
        int grandTotalDown = 0;
        int grandTotalFlat = 0;
        decimal grandSumSimpleChg = 0m;

        foreach (var sector in sectors)
        {
            var sectorStocks = stocks.Where(s => s.SectorId == sector.Id).ToList();
            if (sectorStocks.Count == 0) continue;

            int count = sectorStocks.Count;
            int up = 0;
            int down = 0;
            int flat = 0;
            decimal sumSimpleChg = 0m;
            decimal sectorTotalMv = 0m;
            decimal sumWeightedReturn = 0m;

            var items = new List<(string Ticker, decimal Price, decimal Chg, decimal Mv)>();

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

                sumSimpleChg += chg;
                sectorTotalMv += mv;

                items.Add((st.Ticker, price, chg, mv));
            }

            // Weighted average by Market Value:
            decimal weightedAvg = 0m;
            if (sectorTotalMv > 0m)
            {
                foreach (var it in items)
                {
                    weightedAvg += (it.Mv / sectorTotalMv) * it.Chg;
                }
            }

            decimal simpleAvg = count > 0 ? (sumSimpleChg / count) : 0m;

            grandTotalStocks += count;
            grandTotalUp += up;
            grandTotalDown += down;
            grandTotalFlat += flat;
            grandSumSimpleChg += sumSimpleChg;
            grandTotalMv += sectorTotalMv;

            string simpleStr = $"{simpleAvg:+0.00;-0.00}%";
            string weightedStr = $"{weightedAvg:+0.00;-0.00}%";
            string mvMillion = (sectorTotalMv / 1_000_000m).ToString("N0");

            Console.WriteLine($"{sector.NameAr,-32} | {count,-12} | {up,-4} | {down,-4} | {flat,-4} | {simpleStr,-14} | {weightedStr,-16} | {mvMillion,-20}");
        }

        Console.WriteLine(new string('-', 120));
        decimal grandSimpleAvg = grandTotalStocks > 0 ? (grandSumSimpleChg / grandTotalStocks) : 0m;
        string grandMvMillion = (grandTotalMv / 1_000_000m).ToString("N0");
        Console.WriteLine($"{"الإجمالي",-32} | {grandTotalStocks,-12} | {grandTotalUp,-4} | {grandTotalDown,-4} | {grandTotalFlat,-4} | {$"{grandSimpleAvg:+0.00;-0.00}%",-14} | {"",-16} | {grandMvMillion,-20}");
    }
}
