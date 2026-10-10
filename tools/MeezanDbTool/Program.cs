using System;
using System.Text;
using Microsoft.Data.SqlClient;

public class Program
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        bool apply = args.Length > 0 && args[0].Equals("--apply", StringComparison.OrdinalIgnoreCase);

        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        // 1. Ensure IsManual column exists on Stocks table
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT s.Ticker, s.NameAr, sc.HasShariahBoard, sc.Status, sc.Note
                FROM ShariahCompliances sc
                JOIN Stocks s ON s.Id = sc.StockId
                WHERE sc.HasShariahBoard = 1";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                for (int i = 0; i < r.FieldCount; i++)
                {
                    Console.WriteLine($"{r.GetName(i)}: {r.GetValue(i)}");
                }
                Console.WriteLine("---");
            }
        }
        bool hasIsManual = true;
        if (!hasIsManual)
        {
            Console.WriteLine("Applying 20261008200000_AddStockIsManual migration to database...");
            using var tx = conn.BeginTransaction();
            
            void Exec(string sql)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            Exec("ALTER TABLE Stocks ADD IsManual bit NULL;");
            Exec("UPDATE Stocks SET IsManual = 0 WHERE IsManual IS NULL;");
            Exec("ALTER TABLE Stocks ALTER COLUMN IsManual bit NOT NULL;");
            Exec("ALTER TABLE Stocks ADD DEFAULT (0) FOR IsManual;");
            Exec(@"IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261008200000_AddStockIsManual')
                  BEGIN
                      INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                      VALUES ('20261008200000_AddStockIsManual', '9.0.0');
                  END");
            tx.Commit();
            Console.WriteLine("SUCCESS: Migration 20261008200000_AddStockIsManual applied!");
        }

        Console.WriteLine(apply
            ? "=== RUNNING LIVE MIGRATION (APPLYING CHANGES) ==="
            : "=== RUNNING MIGRATION DRY-RUN (PREVIEW ONLY — PASS --apply TO EXECUTE) ===");

        // We find IndexConstituent rows where weights belong to an index or sector set
        // that sums to about 1.0 (between 0.90 and 1.10) instead of ~100.
        // Specifically for Sectoral-Indices (Index 7), each of the 16 sectors sums to 1.00.
        // Rows with weight <= 1.0 in this set were parsed as decimal fractions and must be multiplied by 100.
        
        var candidates = new List<(int Id, int IndexId, string IndexCode, string Ticker, string NameAr, string Sector, decimal? OldWeight)>();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT ic.Id, ic.IndexId, i.Code, s.Ticker, s.NameAr, ISNULL(sec.NameEn, 'None') as Sector, ic.Weight
                FROM IndexConstituents ic
                JOIN Indices i ON i.Id = ic.IndexId
                JOIN Stocks s ON s.Id = ic.StockId
                LEFT JOIN Sectors sec ON sec.Id = s.SectorId
                WHERE ic.IndexId = 7 AND ic.Weight IS NOT NULL
                ORDER BY s.SectorId, ic.Weight DESC;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int id = r.GetInt32(0);
                int indexId = r.GetInt32(1);
                string indexCode = r.GetString(2);
                string ticker = r.GetString(3);
                string nameAr = r.IsDBNull(4) ? "" : r.GetString(4);
                string sector = r.GetString(5);
                decimal? oldW = r.IsDBNull(6) ? null : r.GetDecimal(6);
                candidates.Add((id, indexId, indexCode, ticker, nameAr, sector, oldW));
            }
        }

        Console.WriteLine($"\nFound {candidates.Count} constituent rows under Index 7 (Sectoral-Indices) with non-null stored Weight.");
        Console.WriteLine("Preview (First 15 rows):");
        for (int i = 0; i < Math.Min(15, candidates.Count); i++)
        {
            var c = candidates[i];
            Console.WriteLine($"  [{i + 1,3}] ID {c.Id,4} | {c.Ticker,-6} | {c.Sector,-25} : Stored Weight = {c.OldWeight}% => Setting to NULL");
        }

        if (apply)
        {
            using var tx = conn.BeginTransaction();
            using var updateCmd = conn.CreateCommand();
            updateCmd.Transaction = tx;
            updateCmd.CommandText = @"
                UPDATE ic
                SET ic.Weight = NULL
                FROM IndexConstituents ic
                WHERE ic.IndexId = 7 AND ic.Weight IS NOT NULL;";
            
            int affected = updateCmd.ExecuteNonQuery();
            tx.Commit();
            Console.WriteLine($"\nSUCCESS: Successfully set Weight = NULL on {affected} constituent rows under Index 7 (Sectoral-Indices).");
        }
        else
        {
            Console.WriteLine("\nDry-run complete. Run with '--apply' to set Weight = NULL for these rows in the database.");
        }

        // Verify state of all indices
        using (var chkCmd = conn.CreateCommand())
        {
            chkCmd.CommandText = @"
                SELECT i.Id, i.Code, i.NameAr,
                       COUNT(ic.Id) AS TotalConstituents,
                       COUNT(ic.Weight) AS WeightedConstituents,
                       ISNULL(SUM(ic.Weight), 0) AS WeightSum
                FROM Indices i
                LEFT JOIN IndexConstituents ic ON ic.IndexId = i.Id
                GROUP BY i.Id, i.Code, i.NameAr
                ORDER BY i.Id;";
            using var r = chkCmd.ExecuteReader();
            Console.WriteLine("\n=== INDICES CONSTITUENT WEIGHT VERIFICATION ===");
            Console.WriteLine($"{"ID",-4} | {"Code",-20} | {"NameAr",-25} | {"Total",-6} | {"Weighted",-8} | {"Weight Sum",-10}");
            Console.WriteLine(new string('-', 85));
            while (r.Read())
            {
                int id = r.GetInt32(0);
                string code = r.GetString(1);
                string nameAr = r.IsDBNull(2) ? "" : r.GetString(2);
                int total = r.GetInt32(3);
                int weighted = r.GetInt32(4);
                decimal sum = r.GetDecimal(5);
                Console.WriteLine($"{id,-4} | {code,-20} | {nameAr,-25} | {total,-6} | {weighted,-8} | {sum:F2}%");
            }
        }
    }
}

