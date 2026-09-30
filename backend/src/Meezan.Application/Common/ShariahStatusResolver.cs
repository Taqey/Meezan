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
/// </summary>
public static class ShariahStatusResolver
{
    /// <summary>
    /// The status to display/filter on for one stock.
    /// <list type="bullet">
    /// <item>نشاط الشركة غير متوافق → <see cref="ShariahStatus.NonCompliant"/> (activity screen wins).</item>
    /// <item>Otherwise → if any board recorded "compliant" → Compliant (wins over stored verdict).</item>
    /// <item>Otherwise → the stock's own stored verdict.</item>
    /// <item>Otherwise → null (no data / no opinion).</item>
    /// </list>
    /// </summary>
    public static ShariahStatus? EffectiveStatus(
        ShariahCompliance? compliance,
        StockShariahMetrics? metrics,
        IEnumerable<ShariahSourceOpinion>? opinions = null)
    {
        // 1. Activity screen wins
        if (metrics?.CoreActivityCompliant == false)
            return ShariahStatus.NonCompliant;

        // 2. Any board says compliant? That wins over stored verdict.
        if (opinions != null)
        {
            foreach (var o in opinions)
            {
                if (o.Status != null && o.Status.ToUpper() == "COMPLIANT")
                    return ShariahStatus.Compliant;
            }
        }

        // 3. Stored compliance row (only if no board says compliant)
        if (compliance?.Status != null)
            return compliance.Status;

        // 4. No data
        return null;
    }
}
