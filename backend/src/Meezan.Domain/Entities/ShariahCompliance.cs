using Meezan.Domain.Enums;

namespace Meezan.Domain.Entities;

public class ShariahCompliance
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public ShariahStatus Status { get; set; }
    public decimal? Pct { get; set; }
    public string? Note { get; set; }

    /// <summary>
    /// True when the stock is overseen by its own Shariah board/committee — whether the
    /// source describes it as a plain "لجنة شرعية" or as an accredited
    /// "هيئة رقابة شرعية داخلية معتمدة". Both cases are treated identically: only the
    /// compliance status plus a board note are shown, while the purification percentage,
    /// the external multi-source opinion panel and the AAOIFI/S&amp;P metrics panel are
    /// suppressed (purification is the board's own internal responsibility).
    /// </summary>
    public bool HasShariahBoard { get; set; }

    public DateTime LastCheckedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Stock? Stock { get; set; }
}
