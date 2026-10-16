namespace Meezan.Domain.Models;

public sealed class EffectiveClassification
{
    public int? NodeId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public List<string> PathSlugs { get; set; } = new();
    public List<string> PathNamesAr { get; set; } = new();
    public List<string> PathNamesEn { get; set; } = new();
    public string Source { get; set; } = "None"; // Manual | Auto | SNDUK | None
    public string Reason { get; set; } = string.Empty;
    public bool NeedsReview { get; set; }
    public List<string> ReviewReasons { get; set; } = new();
    public string DisplayPathAr => string.Join(" \u203a ", PathNamesAr);
}
