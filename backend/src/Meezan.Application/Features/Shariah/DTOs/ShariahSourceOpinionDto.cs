using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Shariah.DTOs;

public class ShariahSourceOpinionDto
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public ShariahSourceKey SourceKey { get; set; }
    public string SourceKeyName => SourceKey.ToString();
    /// <summary>
    /// This board's own verdict for THIS stock, exactly as stored — nothing is invented.
    /// Three-state rule for all 7 boards (FaisalBank, Osoul, HalalBourse, Musaffa, Thndr,
    /// Kashif, HalalInvest), evaluated per (stock, board) pair: listed as compliant →
    /// "compliant"; listed as non-compliant → "non_compliant"; stock not covered by that
    /// board → null (no row / no verdict) = "لا يوجد رأي". Callers must surface null as
    /// "لا يوجد رأي", must never substitute a متوافق / غير متوافق verdict, and must leave
    /// it out of any compliance aggregate (denominator = boards with a recorded opinion).
    /// </summary>
    public string? Status { get; set; }
    public decimal? Percentage { get; set; }
    public string? Note { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime? SourceLastUpdated { get; set; }
    public DateTime FetchedAt { get; set; }
    public string? ExtraData { get; set; }
}
