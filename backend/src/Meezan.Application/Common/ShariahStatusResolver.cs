using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Common;

/// <summary>
/// Single source of truth for a stock's compliance verdict (متوافق / غير متوافق).
///
/// <para>
/// The verdict is a property of <b>that individual stock</b>: first its own نشاط الشركة
/// result (<see cref="StockShariahMetrics.CoreActivityCompliant"/>), and only then the
/// stored ratio/board verdict (<see cref="ShariahCompliance.Status"/>). It is never
/// derived from — and never inherited from — the sector or industry classification, and
/// no stock's result is ever copied onto another stock (a conventional bank and an
/// Islamic bank both under "بنوك" keep their own independent verdicts).
/// </para>
/// <para>
/// Every reader (stock list, index constituents, stock detail) must resolve the status
/// through this rule so list and detail always agree. EF queries that cannot call this
/// method inline the exact same expression — see the mirrored ternaries in
/// StockRepository.QueryPagedAsync and IndexRepository.QueryConstituentsPagedAsync.
/// </para>
/// <para>
/// <b>Universal rule for the 7 boards (FaisalBank, Osoul, HalalBourse, Musaffa, Thndr,
/// Kashif, HalalInvest)</b> — evaluated per (stock, board) pair:
/// board explicitly lists the stock as compliant → Compliant; explicitly non-compliant →
/// NonCompliant; stock not covered by that board → no opinion (absent row / null status,
/// shown as "لا يوجد رأي"). "No opinion" is never turned into NonCompliant — here the
/// missing case simply stays <c>null</c>, and on the opinion side a board with no stored
/// row for a stock is left out of every count and of the aggregate denominator
/// (see ShariahSourceOpinion.Status and the stock-detail "noOpinion" state).
/// </para>
/// <para>
/// <b>Conflict resolution</b>: when board opinions and the stored compliance row disagree,
/// a positive board opinion (at least one "compliant") wins → the stock is Compliant.
/// Only when NO board says compliant does the stored verdict (or null) stand.
/// </para>
/// <para>
/// <b>Standards-based Bourse Halal upgrade</b>: a doubtful Bourse Halal opinion whose
/// stock passes at least one quantitative standard
/// (<see cref="ShariahStandardsEvaluator"/>, mirrored from the frontend
/// <c>shariah-standards.ts</c>) is treated as Compliant. This keeps the list,
/// the filters, the detail verdict and the exports on the same verdict the
/// frontend Bourse Halal card shows.
/// </para>
/// <para>
/// <b>EGX33 membership</b>: being a constituent of the EGX33 Shariah index is a
/// compliance signal with the same weight as a board opinion (this mirrors what
/// the index-upload flow persists). It covers members that have no compliance
/// row and no opinions at all (e.g. FAITA, the USD twin of FAIT) — without it
/// they resolve to null ("غير محدد") even though the Shariah index lists them.
/// </para>
/// <para>
/// <b>Explicit-refusal gap fill</b>: with no stored verdict, at least one
/// explicit board refusal and no counter-signal, the verdict is NonCompliant
/// rather than null. Pending/Blocked (unresolved) opinions block this rule so
/// stocks under review are never forced.
/// </para>
/// </summary>
public static class ShariahStatusResolver
{
    /// <summary>
    /// The status to display/filter on for one stock.
    /// <list type="bullet">
    /// <item>نشاط الشركة غير متوافق → <see cref="ShariahStatus.NonCompliant"/> (activity screen wins).</item>
    /// <item>Otherwise → if any board recorded "compliant" → Compliant (wins over stored verdict).</item>
    /// <item>Otherwise → if Bourse Halal is doubtful and the stock passes ≥1 quantitative standard → Compliant.</item>
    /// <item>Otherwise → if the stock is an EGX33 Shariah-index constituent → Compliant.</item>
    /// <item>Otherwise → if there is no stored verdict, some board explicitly refused and none is pending → NonCompliant.</item>
    /// <item>Otherwise → the stock's own stored verdict.</item>
    /// <item>Otherwise → null (no data / no opinion).</item>
    /// </list>
    /// </summary>
    public static ShariahStatus? EffectiveStatus(
        ShariahCompliance? compliance,
        StockShariahMetrics? metrics,
        IEnumerable<ShariahSourceOpinion>? opinions = null,
        bool isShariahIndexMember = false)
    {
        // 1. Activity screen wins (use effective value: override wins over feed)
        if (metrics?.EffectiveCoreActivityCompliant == false)
            return ShariahStatus.NonCompliant;

        // 2. Any board says compliant? That wins over stored verdict.
        //    Exception: HalalBourse "compliant" with percentage < 100 is treated as
        //    doubtful (not compliant), so it does NOT trigger the compliant early-return.
        if (opinions != null)
        {
            foreach (var o in opinions)
            {
                if (o.Status != null && o.Status.ToUpper() == "COMPLIANT")
                {
                    // HalalBourse-specific rule: percentage must be 100 to count as compliant.
                    if (o.SourceKey == ShariahSourceKey.HalalBourse
                        && (o.Percentage == null || o.Percentage < 100))
                        continue; // treat as doubtful — do not elevate to compliant

                    return ShariahStatus.Compliant;
                }
            }
        }

        // 2b. Standards-based Bourse Halal upgrade (mirrors the frontend card rule).
        // Inputs mirror the frontend "AAOIFI & S&P detailed ratios" cells:
        // revenue = SpHaramEarningPercentage ?? purification Pct,
        // debt = LoansPercentage (no numeric investments/cash columns exist —
        // those criteria are always "no data" and excluded here too).
        if (ShariahStandardsEvaluator.HasDoubtfulBourseHalal(opinions))
        {
            var revenue = metrics?.EffectiveHaramRevenuePercentage ?? compliance?.Pct;
            var debt = metrics?.EffectiveLoansPercentage;
            if (ShariahStandardsEvaluator.PassesAnyStandard(revenue, debt, null, null))
                return ShariahStatus.Compliant;
        }

        // 2c. EGX33 Shariah-index membership: same weight as a board opinion.
        if (isShariahIndexMember)
            return ShariahStatus.Compliant;

        // 2d. Explicit refusal with no counter-signal and no stored verdict.
        if (compliance == null && opinions != null)
        {
            var refused = false;
            var unresolved = false;
            foreach (var o in opinions)
            {
                var s = NormalizeOpinionStatus(o.Status);
                if (s == "NONCOMPLIANT") refused = true;
                else if (s == "PENDING" || s == "BLOCKED") unresolved = true;
            }
            if (refused && !unresolved) return ShariahStatus.NonCompliant;
        }

        // 3. Stored compliance row (only if no board says compliant)
        if (compliance?.Status != null)
            return compliance.Status;

        // 4. No data
        return null;
    }

    /// <summary>
    /// True for the EGX33 Shariah index codes in any historical spelling
    /// ("Shariah", "EGX33", "EGX 33"). The EF query mirrors below compare the
    /// raw upper-cased codes instead (expression trees cannot call this).
    /// </summary>
    public static bool IsShariahIndexCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var normalized = code.Trim().ToUpperInvariant().Replace(" ", string.Empty);
        return normalized == "SHARIAH" || normalized == "EGX33";
    }

    /// <summary>
    /// Case/separator-insensitive opinion status ("non_compliant",
    /// "non-compliant" and "NonCompliant" are the same refusal).
    /// </summary>
    public static string NormalizeOpinionStatus(string? status)
        => (status ?? string.Empty).Trim().ToUpperInvariant()
            .Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
}
