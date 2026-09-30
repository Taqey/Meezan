using System.Text.Json;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Tools.ShariahAudit;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var conn = ReadConnectionString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(conn)
            .Options;

        await using var db = new ApplicationDbContext(options);

        var stocks = await db.Stocks
            .Include(s => s.ShariahCompliance)
            .Include(s => s.ShariahMetrics)
            .Include(s => s.ShariahSourceOpinions)
            .Include(s => s.IndexConstituents)
                .ThenInclude(ic => ic.Index)
            .ToListAsync();

        int hasCompliance = stocks.Count(s => s.ShariahCompliance != null);
        int missingCompliance = stocks.Count(s => s.ShariahCompliance == null);
        Console.WriteLine($"Total stocks: {stocks.Count}, With Compliance: {hasCompliance}, Without Compliance: {missingCompliance}");

        var withoutCompliance = stocks.Where(s => s.ShariahCompliance == null).ToList();
        Console.WriteLine("\nSTOCKS WITHOUT ShariahCompliance ROW:");
        foreach (var s in withoutCompliance)
        {
            var inEgx33 = s.IndexConstituents.Any(ic => ic.Index != null && (ic.Index.Code.Trim() == "EGX 33" || ic.Index.Code.Trim() == "EGX33" || ic.Index.Code.Trim() == "Shariah"));
            var boardCompliantCount = s.ShariahSourceOpinions.Count(o => o.Status != null && o.Status.ToUpper() == "COMPLIANT");
            Console.WriteLine($"  {s.Ticker}: InEGX33={inEgx33}, BoardCompliant={boardCompliantCount}, Opinions={s.ShariahSourceOpinions.Count}, Activity={s.ShariahMetrics?.CoreActivityCompliant}");
        }
    }

    private static string ReadConnectionString()
    {
        var appSettings = Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "Meezan.WebApi", "appsettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(appSettings));
        return doc.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString()!;
    }
}
