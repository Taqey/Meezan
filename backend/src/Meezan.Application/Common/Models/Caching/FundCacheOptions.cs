namespace Meezan.Application.Common.Models.Caching;

public class FundCacheOptions
{
    public const string SectionName = "Funds:Cache";

    public bool Enabled { get; set; } = true;

    public bool WarmupOnStartup { get; set; } = true;
}
