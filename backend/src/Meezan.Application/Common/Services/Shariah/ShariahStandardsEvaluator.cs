using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Common.Services.Shariah;

/// <summary>
/// Quantitative Shariah screening standards — backend mirror of the frontend
/// single source of truth (<c>frontend/src/app/models/shariah-standards.ts</c>).
/// Thresholds are duplicated here (not referenced) because filtering and
/// sorting run inside SQL via EF expression trees, which cannot call into the
/// frontend. Keep both files in sync: same five standards, same
/// "ratio ≤ max" (inclusive) rule, same missing-data semantics.
/// </summary>
public static class ShariahStandardsEvaluator
{
    public sealed record StandardThresholds(
        decimal ProhibitedRevenueMax,
        decimal DebtMax,
        decimal ProhibitedInvestmentsMax,
        decimal CashMax);

    public static readonly StandardThresholds Egx33 = new(10, 33, 33, 70);
    public static readonly StandardThresholds Dfm = new(10, 30, 30, 70);
    public static readonly StandardThresholds Aaoifi = new(5, 30, 30, 70);
    public static readonly StandardThresholds Sp = new(5, 33, 33, 70);
    public static readonly StandardThresholds Klsi = new(20, 33, 33, 70);

    /// <summary>Single source of truth for thresholds — used by the shared verdict service. No duplicates.</summary>
    public static IReadOnlyList<StandardThresholds> AllThresholds => All;

    private static readonly StandardThresholds[] All = [Egx33, Dfm, Aaoifi, Sp, Klsi];

    /// <summary>
    /// True when the stock passes AT LEAST ONE standard. A standard passes when
    /// all of its AVAILABLE criteria pass; a missing/null ratio is "no data" —
    /// excluded from the decision, never treated as 0. A standard with no
    /// available criteria at all never passes.
    /// </summary>
    public static bool PassesAnyStandard(
        decimal? prohibitedRevenue,
        decimal? debt,
        decimal? prohibitedInvestments,
        decimal? cash)
    {
        foreach (var t in All)
        {
            if (Passes(t, prohibitedRevenue, debt, prohibitedInvestments, cash))
                return true;
        }
        return false;
    }

    private static bool Passes(
        StandardThresholds t,
        decimal? revenue,
        decimal? debt,
        decimal? investments,
        decimal? cash)
    {
        var available = 0;
        if (!CriterionPasses(revenue, t.ProhibitedRevenueMax, ref available)) return false;
        if (!CriterionPasses(debt, t.DebtMax, ref available)) return false;
        if (!CriterionPasses(investments, t.ProhibitedInvestmentsMax, ref available)) return false;
        if (!CriterionPasses(cash, t.CashMax, ref available)) return false;
        return available > 0;
    }

    private static bool CriterionPasses(decimal? value, decimal max, ref int available)
    {
        if (value == null) return true; // no data → excluded, never 0
        available++;
        return value.Value <= max;
    }

    /// <summary>
    /// The frontend "Bourse Halal card" doubtful state for one opinion:
    /// explicitly doubtful, or compliant with percentage &lt; 100 (or missing),
    /// which the display treats as doubtful (مشكوك).
    /// </summary>
    public static bool IsDoubtfulBourseHalalOpinion(string? status, decimal? percentage)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var s = status.Trim().ToUpperInvariant();
        if (s == "DOUBTFUL") return true;
        return s == "COMPLIANT" && (percentage == null || percentage < 100);
    }

    /// <summary>
    /// True when the stock has a Bourse Halal opinion whose display state is doubtful.
    /// </summary>
    public static bool HasDoubtfulBourseHalal(IEnumerable<ShariahSourceOpinion>? opinions)
    {
        if (opinions == null) return false;
        foreach (var o in opinions)
        {
            if (o.SourceKey == ShariahSourceKey.HalalBourse
                && IsDoubtfulBourseHalalOpinion(o.Status, o.Percentage))
                return true;
        }
        return false;
    }
}
