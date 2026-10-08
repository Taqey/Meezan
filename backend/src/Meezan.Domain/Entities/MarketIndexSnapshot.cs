namespace Meezan.Domain.Entities;

/// <summary>
/// Last good published quote scraped from Mubasher for one index or sector:
/// value, daily change and daily change %, plus the source's own date text.
/// Only overwritten on a successful parse — a failed fetch keeps serving the
/// previous snapshot, so consumers never see a hole where data existed.
/// </summary>
public class MarketIndexSnapshot
{
    public int Id { get; set; }

    /// <summary>"Index" or "Sector".</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// OUR stable key: the Index.Code from our Indices table
    /// ("EGX 33", "EGX30", …) or the Sector.Id as string.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Mubasher slug this was scraped from (e.g. "EGX30", "REAL").</summary>
    public string MubasherCode { get; set; } = string.Empty;

    public decimal? Value { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePct { get; set; }

    /// <summary>Source's own date line, e.g. "الثلاثاء, أكتوبر 06".</summary>
    public string? SourceDateText { get; set; }

    /// <summary>Always "Mubasher" for now — kept explicit for attribution.</summary>
    public string Source { get; set; } = "Mubasher";

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
