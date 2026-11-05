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

public static class InvestigateDifferences
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static async Task RunAsync()
    {
        Console.WriteLine("=== INVESTIGATING DIFFERENCES ===");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(ConnStr);

        await using var db = new ApplicationDbContext(optionsBuilder.Options);

        var sector = await db.Sectors.FirstOrDefaultAsync(s => s.NameAr.Contains("أساسية"));
        if (sector == null) return;

        var stocks = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Where(s => s.SectorId == sector.Id && s.IsActive && s.DataStatus == StockDataStatus.Active)
            .ToListAsync();

        Console.WriteLine($"Basic Materials stocks ({stocks.Count}):");
        decimal totalMv = 0m;
        foreach (var s in stocks)
        {
            var p = s.MarketData?.ClosingPrice ?? 0m;
            var c = s.SupportResistance?.ChangePct ?? 0m;
            var sh = s.MarketData?.SharesOutstanding ?? 0m;
            var mv = p * sh;
            totalMv += mv;
            Console.WriteLine($"{s.Ticker,-10} | Price: {p,8:F2} | Chg: {c,6:F2}% | Shares: {sh,14:N0} | Mv: {mv / 1_000_000m,10:N1}M");
        }

        Console.WriteLine($"Total Sector MV: {totalMv / 1_000_000m:N1}M");
        decimal weightedSum = 0m;
        foreach (var s in stocks)
        {
            var p = s.MarketData?.ClosingPrice ?? 0m;
            var c = s.SupportResistance?.ChangePct ?? 0m;
            var sh = s.MarketData?.SharesOutstanding ?? 0m;
            var mv = p * sh;
            var w = totalMv > 0 ? (mv / totalMv) : 0m;
            weightedSum += w * c;
        }
        Console.WriteLine($"Calculated Weighted Return: {weightedSum:+0.00;-0.00}%");

        // Check SectorDaily table row
        var sd = await db.SectorDailies
            .Where(s => s.SectorId == sector.Id)
            .OrderByDescending(s => s.TradingDate)
            .FirstOrDefaultAsync();

        if (sd != null)
        {
            Console.WriteLine($"DB SectorDaily row: Date: {sd.TradingDate:yyyy-MM-dd}, ChangePct: {sd.ChangePct}%, Uncapped: {sd.ChangePctUncapped}%");
        }
    }
}
