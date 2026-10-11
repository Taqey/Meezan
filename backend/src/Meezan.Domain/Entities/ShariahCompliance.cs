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

    /// <summary>
    /// True when the stored verdict is Compliant ONLY through the individual-scholars
    /// debt relaxation (debt ≤ 50% with all non-debt ratios passing on ≥1 standard).
    /// Treated as a normal Compliant everywhere except the detail-page note.
    /// </summary>
    public bool ViaScholarsOnly { get; set; }

    /// <summary>
    /// The Halal Bourse source's OWN verdict from our stored ratio evaluation
    /// (Compliant / NonCompliant / Pending = Insufficient data). Null when never
    /// computed (row predates the backfill). The raw source value (doubtful/مشكوك)
    /// is sanitized away and never counts.
    /// </summary>
    public ShariahStatus? HalalStatus { get; set; }

    /// <summary>
    /// Which path produced the overall <see cref="Status"/>: Activity, Boards,
    /// Halal, HalalScholars, EGX33, BoardsFallback (ratios missing, other sources
    /// decided), HalalNonCompliant, GapFill, Insufficient, Stored or Unknown.
    /// </summary>
    public string? VerdictSource { get; set; }

    /// <summary>
    /// JSON blob with the stored per-criterion results (pass/fail/skipped per ratio
    /// and per each of the 6 criteria, plus passed counts and effective ratios).
    /// Written only by <c>ShariahVerdictService</c>; read directly, never recomputed.
    /// </summary>
    public string? VerdictDetailsJson { get; set; }

    public Stock? Stock { get; set; }
}
