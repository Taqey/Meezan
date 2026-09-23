namespace Meezan.Domain.Entities;

public class ShariahRefreshLog
{
    public int Id { get; set; }
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public int PctUpdatedCount { get; set; }
    public int SkippedNoValueCount { get; set; }
    public int SkippedNotFoundCount { get; set; }
    public int StocksFullyRefreshedCount { get; set; }
    public string TriggeredBy { get; set; } = "Scheduled"; // "Scheduled" or "Manual"
}
