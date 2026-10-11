using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Common;

/// <summary>
/// Single source of truth for a stock's DISPLAYED overall verdict
/// ("الحكم الداخلي المعتمد"): the STORED overall verdict computed once via
/// <see cref="ShariahVerdictService.ComputeOverall"/> and persisted on
/// <see cref="ShariahCompliance"/> (Status + VerdictSource).
/// Readers must call this (or read compliance.Status directly) — never recompute.
/// <para>
/// The overall verdict aggregates all independent sources: the external Sharia
/// bodies, EGX33 index membership, and the Halal Bourse source — where the Halal
/// Bourse input is the STORED ratio verdict (HalalStatus), never the raw source
/// value. Raw "doubtful"/"مشكوك" never counts (sanitized to no opinion).
/// Halal Insufficient (Pending) excludes Halal from the aggregation: other
/// sources decide; with no compliant other source the result is Insufficient
/// (Pending), never NonCompliant.
/// </para>
/// <para>
/// Stored <see cref="ShariahStatus.Pending"/> means "البيانات غير كافية"
/// (Insufficient data): neutral, never counted as compliant, own filter option.
/// </para>
/// </summary>
public static class ShariahStatusResolver
{
    /// <summary>
    /// The status to display/filter on for one stock (restored aggregation with
    /// the Halal input taken from the stored ratio verdict).
    /// </summary>
    public static ShariahStatus? EffectiveStatus(
        ShariahCompliance? compliance,
        StockShariahMetrics? metrics,
        IEnumerable<ShariahSourceOpinion>? opinions = null,
        bool isShariahIndexMember = false)
        => ResolveWithSource(compliance, metrics, opinions, isShariahIndexMember).Status;

    /// <summary>
    /// Which path produced <see cref="EffectiveStatus"/> (for persist + header notes).
    /// </summary>
    public static string EffectiveSource(
        ShariahCompliance? compliance,
        StockShariahMetrics? metrics,
        IEnumerable<ShariahSourceOpinion>? opinions = null,
        bool isShariahIndexMember = false)
        => ResolveWithSource(compliance, metrics, opinions, isShariahIndexMember).Source;

    private static (ShariahStatus? Status, string Source) ResolveWithSource(
        ShariahCompliance? compliance,
        StockShariahMetrics? metrics,
        IEnumerable<ShariahSourceOpinion>? opinions,
        bool isShariahIndexMember)
    {
        var activity = metrics?.EffectiveCoreActivityCompliant;
        if (activity == false)
            return (ShariahStatus.NonCompliant, ShariahVerdictService.OverallSource.Activity);

        var others = (opinions ?? Enumerable.Empty<ShariahSourceOpinion>())
            .Where(o => o.SourceKey != ShariahSourceKey.HalalBourse)
            .Select(o => o.Status);
        return ShariahVerdictService.ComputeOverall(
            compliance?.HalalStatus,
            compliance?.ViaScholarsOnly ?? false,
            others,
            isShariahIndexMember,
            activity,
            compliance?.Status,
            hasStoredRow: compliance != null);
    }

    /// <summary>
    /// True for the EGX33 Shariah index codes in any historical spelling
    /// ("Shariah", "EGX33", "EGX 33"). Membership is an aggregation input
    /// (same weight as a board opinion) AND drives the informational 8th card.
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
    /// Doubtful/مشكوك is NOT a refusal — callers must treat it as no opinion
    /// (see ShariahVerdictService.SanitizeOpinionStatus).
    /// </summary>
    public static string NormalizeOpinionStatus(string? status)
        => (status ?? string.Empty).Trim().ToUpperInvariant()
            .Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
}
