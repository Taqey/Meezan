using System;
using System.Text;
using Microsoft.Data.SqlClient;

public class Program
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        // Step 1: Show KORRA's current status BEFORE the fix
        Console.WriteLine("=== BEFORE fix ===");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT s.Ticker, s.NameAr, sc.Status, sc.Pct, sc.Note, sc.HasShariahBoard
                FROM Stocks s
                LEFT JOIN ShariahCompliances sc ON sc.StockId = s.Id
                WHERE s.Ticker = 'KORRA';";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Console.WriteLine($"KORRA ShariahCompliance -> Ticker: {r[0]}, Name: {r[1]}, Status: {r[2]}, Pct: {r[3]}, Note: {r[4]}, HasBoard: {r[5]}");
            }
        }

        // Step 2: Apply fix — change KORRA Status from Compliant (1) to Pending (3)
        // Only applies if still Compliant (idempotent)
        Console.WriteLine("\n=== Applying fix: KORRA Compliant -> Pending ===");
        int rowsAffected;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                UPDATE sc
                SET sc.Status = 'Pending', sc.UpdatedAt = GETUTCDATE()
                FROM ShariahCompliances sc
                JOIN Stocks s ON s.Id = sc.StockId
                WHERE s.Ticker = 'KORRA' AND sc.Status = 'Compliant';";
            rowsAffected = cmd.ExecuteNonQuery();
        }
        Console.WriteLine($"  Rows updated: {rowsAffected}");

        // Step 3: Verify
        Console.WriteLine("\n=== AFTER fix ===");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT s.Ticker, s.NameAr, sc.Status, sc.Pct, sc.Note, sc.HasShariahBoard
                FROM Stocks s
                LEFT JOIN ShariahCompliances sc ON sc.StockId = s.Id
                WHERE s.Ticker = 'KORRA';";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Console.WriteLine($"KORRA ShariahCompliance -> Ticker: {r[0]}, Name: {r[1]}, Status: {r[2]}, Pct: {r[3]}, Note: {r[4]}, HasBoard: {r[5]}");
            }
        }
    }
}

