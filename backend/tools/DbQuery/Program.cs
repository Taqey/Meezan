using System;
using Microsoft.Data.SqlClient;

class Program {
    static void Print(SqlConnection conn, string label, string sql) {
        Console.WriteLine($"---- {label} ----");
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        var cols = new string[r.FieldCount];
        for (int i = 0; i < r.FieldCount; i++) cols[i] = r.GetName(i);
        Console.WriteLine(string.Join(" | ", cols));
        int n = 0;
        while (r.Read()) {
            var vals = new object[r.FieldCount];
            for (int i = 0; i < r.FieldCount; i++) {
                var v = r.IsDBNull(i) ? "NULL" : r.GetValue(i)?.ToString();
                if (v != null && v.Length > 120) v = v.Substring(0, 120);
                vals[i] = v;
            }
            Console.WriteLine(string.Join(" | ", vals));
            if (++n > 200) { Console.WriteLine("... (truncated)"); break; }
        }
        Console.WriteLine();
    }
    static void Main() {
        var connStr = "Server=db69223.public.databaseasp.net; Database=db69223; User Id=db69223; Password=Qq4#s8+BY!t6; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";
        using var conn = new SqlConnection(connStr);
        conn.Open();
        Print(conn, "CommoditiesFunds", "SELECT Id, Code, NameAr, NameEn, Type, TypeNameAr, Slug FROM Funds WHERE IsActive=1 AND Type=3 ORDER BY Code");
        Print(conn, "IndexFunds", "SELECT Id, Code, NameAr, Type, Slug FROM Funds WHERE IsActive=1 AND Type=8 ORDER BY Code");
        Print(conn, "TypeOverridesAll", "SELECT o.FundId, f.Code, o.FundTypeDefinitionId, d.Slug, d.NameAr, o.CreatedBy, o.CreatedAt FROM FundTypeOverrides o JOIN Funds f ON f.Id=o.FundId JOIN FundTypeDefinitions d ON d.Id=o.FundTypeDefinitionId ORDER BY o.CreatedAt");
        Print(conn, "TypeChangeLog", "SELECT TOP 20 Id, FundId, FundTypeDefinitionId, Action, PerformedBy, Reason, CreatedAt FROM FundTypeChangeLogs ORDER BY Id DESC");
        Print(conn, "CategoryChangeLog", "SELECT TOP 20 Id, FundId, CategoryId, Action, PerformedBy, Reason, CreatedAt FROM FundCategoryChangeLogs ORDER BY Id DESC");
        Print(conn, "SyncSettings", "SELECT Id, IsEnabled, RunTimeCairo, PreventOverlap, RequestDelaySeconds, MaxRetries, RetryBackoffSeconds FROM FundSyncSettings");
        Print(conn, "SyncSettingsCols", "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FundSyncSettings' ORDER BY ORDINAL_POSITION");
        Print(conn, "AdminCategoryOverrides", "SELECT o.FundId, f.Code, o.CategoryId, c.Slug, o.Action, o.CreatedBy, o.CreatedAt FROM FundCategoryOverrides o JOIN Funds f ON f.Id=o.FundId JOIN FundCategories c ON c.Id=o.CategoryId WHERE o.CreatedBy<>'AutoSync' ORDER BY o.CreatedAt DESC");
        Print(conn, "ClassTables", "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'FundClassification%' ORDER BY TABLE_NAME");
        conn.Close();
    }
}
