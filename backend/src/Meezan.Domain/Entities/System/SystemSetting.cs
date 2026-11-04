namespace Meezan.Domain.Entities;

/// <summary>
/// General system key-value configuration setting (e.g. USD_EGP_RATE, SECTOR_CHANGE_WEIGHTING).
/// </summary>
public class SystemSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
