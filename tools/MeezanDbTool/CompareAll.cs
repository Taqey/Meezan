using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;

namespace MeezanDbTool;

public class CompareAll
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public class StartaItem
    {
        public string symbol { get; set; } = "";
        public string nameEn { get; set; } = "";
        public string nameAr { get; set; } = "";
        public string rawSector { get; set; } = "";
        public string rawPrice { get; set; } = "";
        public string rawChange { get; set; } = "";
    }

    public static async Task RunAsync()
    {
        var json = File.ReadAllText("tools/starta_parsed.json");
        var startaList = JsonSerializer.Deserialize<List<StartaItem>>(json)!;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnStr)
            .Options;
        using var db = new ApplicationDbContext(options);

        var dbStocks = await db.Stocks
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Include(s => s.Sector)
            .ToListAsync();

        var dbMap = dbStocks.ToDictionary(s => s.Ticker.ToUpperInvariant(), s => s);

        var notInDb = startaList.Where(s => !dbMap.ContainsKey(s.symbol.ToUpperInvariant())).ToList();

        Console.WriteLine($"=== STARTA COMPANIES NOT IN DB ({notInDb.Count} rows) ===");
        foreach (var s in notInDb)
        {
            // check alias
            var cand = dbStocks.FirstOrDefault(d => 
                (s.symbol.StartsWith("EGS") && d.NameAr != null && s.nameAr != null && s.nameAr.Length > 2 && d.NameAr.Contains(s.nameAr)) ||
                (s.symbol == "AIH" && d.Ticker == "AIHC") ||
                (s.symbol == "TYCN" && d.Ticker == "ANFI") ||
                (s.symbol == "NDRL" && d.Ticker.StartsWith("NDRL")) ||
                (s.symbol == "KORA" && d.Ticker.StartsWith("KORA")) ||
                (s.symbol == "EGS385S1C012" && d.Ticker == "FERC") ||
                (s.symbol == "EGS370O1C013" && d.Ticker == "NAPR")
            );

            string candStr = cand != null ? $"--> ALIAS: {cand.Ticker} ({cand.NameAr})" : "--> TRULY NEW (Insert)";
            Console.WriteLine($"{s.symbol,-14} | Price: {s.rawPrice,-10} | Chg: {s.rawChange,-8} | Sector: {s.rawSector,-35} | Name: {s.nameAr} / {s.nameEn} {candStr}");
        }
    }
}

