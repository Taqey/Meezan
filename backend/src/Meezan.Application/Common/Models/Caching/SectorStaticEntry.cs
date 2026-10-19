namespace Meezan.Application.Common.Models.Caching;

/// <summary>
/// Static dataset of sectors (Id, NameAr, NameEn).
/// </summary>
public sealed class SectorStaticEntry
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
}
