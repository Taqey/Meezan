namespace Meezan.Domain.Models;

public sealed class ClassificationMigrationReport
{
    public bool SeedPerformed { get; set; }
    public bool BackupPerformed { get; set; }
    public int BackupRows { get; set; }
    public List<ChangedFund> Changed { get; set; } = new();
    public List<string> UnclassifiedEquity { get; set; } = new();
    public int CommoditiesRenamed { get; set; }
    public List<string> CommoditiesFunds { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}
