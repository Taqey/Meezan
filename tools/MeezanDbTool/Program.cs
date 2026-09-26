using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;
using Meezan.Infrastructure.Services;

public class Program
{
    private const string ConnStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

    public static void Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Console.WriteLine("=================================================");
        Console.WriteLine("STEP 1: ESTABLISH THE REAL BASELINE FIRST");
        Console.WriteLine("=================================================");
        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Stocks";
            int total = (int)cmd.ExecuteScalar();
            Console.WriteLine($"SELECT COUNT(*) FROM Stocks -> {total}");
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Stocks WHERE IsActive = 1";
            int active = (int)cmd.ExecuteScalar();
            Console.WriteLine($"SELECT COUNT(*) FROM Stocks WHERE IsActive = 1 -> {active}");
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Stocks WHERE IsActive = 0";
            int deactivated = (int)cmd.ExecuteScalar();
            Console.WriteLine($"SELECT COUNT(*) FROM Stocks WHERE IsActive = 0 -> {deactivated}");
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Ticker, DeactivatedAt, DeactivationReason FROM Stocks WHERE IsActive = 0";
            using var reader = cmd.ExecuteReader();
            Console.WriteLine("\n--- DEACTIVATED ROWS IN STOCKS TABLE ---");
            int cnt = 0;
            while (reader.Read())
            {
                cnt++;
                string ticker = reader.GetString(0);
                string deactAt = reader.IsDBNull(1) ? "NULL" : reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss");
                string reason = reader.IsDBNull(2) ? "NULL" : reader.GetString(2);
                Console.WriteLine($"[{cnt}] Ticker: {ticker} | DeactivatedAt: {deactAt} | Reason: {reason}");
            }
            if (cnt == 0) Console.WriteLine("None (0 deactivated rows found).");
        }

        // Also check if any PDF tickers are missing
        var faisalTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FAIT", "SAUD", "ADIB", "EGAL", "ATQA", "SKPC", "RMDA", "ISPH", "TMGH", "MASR",
            "OCDI", "PHDC", "ORHD", "EGAS", "RACC", "ETEL", "EFIH", "JUFO", "MPCO", "OLFI",
            "EFID", "AMOC", "MTIE", "IFAP", "ICFC", "ETRS", "CIRA", "ORAS", "ACGC", "ORWE",
            "ARCC", "LCSW", "MCQE", "ISMQ", "FERC", "EFIC", "ALUM", "NIPH", "CPCI", "AMES",
            "SIPC", "CLHO", "BIOC", "AXPH", "OCPH", "ADCI", "SPMD", "EEII", "MBEG", "IDRE",
            "ACAP", "NARE", "AIFI", "RREI", "DAPH", "OBRI", "AREH", "ROTO", "DGTZ", "INFI",
            "EPCO", "MILS", "UEFM", "COSG", "MOSC", "GGRN", "ISMA", "POUL", "ZEOT", "AJWA",
            "DOMT", "GMCI", "GOUR", "MOED", "CAED", "TALM", "ODIN", "CRST", "AALR", "IEEC",
            "EALR", "APSW", "SPIN", "KABO", "RUBX", "SCEM", "MBSC", "PRCL", "ARVA", "UBEE",
            "AMIA", "ATLC"
        };

        var ostoulTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "JUFO", "ETEL", "RACC", "COSG", "MPCO", "LUTS", "ISMA", "AJWA", "ZEOT", "MTIE",
            "IFAP", "ICFC", "ETRS", "CIRA", "TALM", "MOED", "SWDY", "KRDI", "ISPH", "CLHO",
            "RMDA", "BIOC", "MCRO", "SPMD", "ADPC", "CRST", "IEEC", "ORAS", "ORWE", "KABO",
            "SPIN", "EKHO", "EGAS", "AMOC", "ARCC", "LCSW", "SVCE", "CERA", "PRCL", "MBSC",
            "MFPC", "ATQA", "EGAL", "SKPC", "SIPC", "NIPH", "ADIB", "FAIT", "SAUD", "ATLC",
            "AMIA", "EFIH", "FWRY", "SUGR", "POUL", "OLFI", "EFID", "ELEC", "PHAR", "CSAG",
            "ALCN", "ENGC", "MEPA", "NCCW", "DSCW", "ACGC", "CCAP", "RAYA", "SDTI", "EGTS",
            "SCTS", "MOIL", "MCQE", "SCEM", "TAQA", "ABUK", "ASCM", "KZPC", "EGCH", "ECAP",
            "MICH", "ISMQ", "EFIC", "UNIP", "MPCI"
        };

        var existingInDb = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Ticker FROM Stocks";
            using var r = cmd.ExecuteReader();
            while (r.Read()) existingInDb.Add(r.GetString(0).Trim().ToUpperInvariant());
        }

        var missingFromPdf = faisalTickers.Concat(ostoulTickers)
            .Where(t => !existingInDb.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingFromPdf.Count > 0)
        {
            Console.WriteLine($"\nFound {missingFromPdf.Count} tickers from PDFs not yet in DB: {string.Join(", ", missingFromPdf)}");
            foreach (var t in missingFromPdf)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Stocks (Ticker, NameAr, NameEn, IsActive, CreatedAt, UpdatedAt)
                    VALUES (@t, @t, @t, 1, SYSUTCDATETIME(), SYSUTCDATETIME());";
                cmd.Parameters.Add(new SqlParameter("@t", t));
                cmd.ExecuteNonQuery();
                Console.WriteLine($"  -> Inserted {t}");
            }
        }
        else
        {
            Console.WriteLine("\nAll tickers from both Faisal and Ostoul PDFs exist in the database!");
        }

        Console.WriteLine("\n=================================================");
        Console.WriteLine("FINAL VERIFICATION AFTER ALL SOURCES:");
        Console.WriteLine("=================================================");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Stocks; SELECT COUNT(*) FROM Stocks WHERE IsActive = 1; SELECT COUNT(*) FROM Stocks WHERE IsActive = 0;";
            using var r = cmd.ExecuteReader();
            r.Read();
            int total = r.GetInt32(0);
            r.NextResult();
            r.Read();
            int active = r.GetInt32(0);
            r.NextResult();
            r.Read();
            int deactivated = r.GetInt32(0);
            Console.WriteLine($"Total Stocks (all): {total}");
            Console.WriteLine($"Active Stocks (IsActive = 1): {active}");
            Console.WriteLine($"Deactivated Stocks (IsActive = 0): {deactivated}");
        }
    }
}
