namespace Meezan.Application.Common.Models.Caching;

/// <summary>
/// Static snapshot of one index and its active constituents. <see cref="Complete"/> is the
/// completeness marker: an entry without it (or without rows) is treated as a cache miss.
/// </summary>
public sealed class IndexStaticEntry
{
    public int IndexId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public List<IndexStaticRow> Rows { get; set; } = new();
    public bool Complete { get; set; }
}
