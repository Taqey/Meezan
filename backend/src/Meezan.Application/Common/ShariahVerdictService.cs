using System.Text.Json;
using System.Text.Json.Serialization;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Meezan.Application.Common;

/// <summary>
/// ONE shared Shariah verdict service — the single place that computes verdicts.
/// Two levels, same service:
/// <list type="bullet">
/// <item>Halal Bourse source verdict (<see cref="Compute"/>): quantitative ONLY,
/// from effective ratios. Insufficient (Pending) when debt OR revenue is missing;
/// otherwise Compliant (possibly ViaScholarsOnly) or NonCompliant.</item>
/// <item>Overall internal verdict (<see cref="ComputeOverall"/>): restored
/// board/EGX33/gap-fill aggregation across all independent sources, where the
/// Halal Bourse input is the STORED ratio verdict (raw doubtful/مشكوك never
/// counts). Halal Insufficient excludes Halal from the aggregation.</item>
/// </list>
/// All writers (manual overrides, data pulls, imports, uploads, backfill,
/// threshold changes) must go through here; all readers must read the stored
/// <see cref="ShariahCompliance"/> fields directly with no recomputation.
/// Thresholds live ONLY in <see cref="ShariahStandardsEvaluator"/> — no duplicates.
/// </summary>
public static class ShariahVerdictService
{
    public const decimal ScholarsDebtMax = 50m;

    /// <summary>Which path produced the overall verdict (stored on VerdictSource).</summary>
    public static class OverallSource
    {
        public const string Activity = "Activity";
        public const string Boards = "Boards";
        public const string Halal = "Halal";
        public const string HalalScholars = "HalalScholars";
        public const string EGX33 = "EGX33";
        public const string BoardsFallback = "BoardsFallback";
        public const string HalalNonCompliant = "HalalNonCompliant";
        public const string GapFill = "GapFill";
        public const string Insufficient = "Insufficient";
        public const string Stored = "Stored";
        public const string Unknown = "Unknown";
    }

    public sealed record RatioCheck(
        string Key,
        decimal? Value,
        decimal Max,
        bool HasData,
        bool Passed,
        string Outcome);

    public sealed record CriterionResult(
        string Id,
        string NameEn,
        bool Passed,
        bool HasData,
        List<RatioCheck> Ratios);

    public sealed record VerdictResult(
        ShariahStatus Status,
        bool ViaScholarsOnly,
        int PassedCount,
        int TotalCount,
        decimal? EffectiveRevenue,
        decimal? EffectiveDebt,
        decimal? EffectiveInvestments,
        decimal? EffectiveCash,
        List<string> MissingRatios,
        List<CriterionResult> Criteria);

    public static (decimal? revenue, decimal? debt) EffectiveRatios(StockShariahMetrics? metrics, decimal? compliancePctFallback = null)
    {
        if (metrics is null)
        {
            // Fallback keeps legacy rows (compliance Pct only) evaluable as revenue;
            // debt stays missing → Insufficient unless debt also present.
            return (compliancePctFallback, null);
        }
        decimal? revenue = metrics.EffectiveHaramRevenuePercentage
            ?? metrics.SpHaramEarningPercentage
            ?? compliancePctFallback;
        decimal? debt = metrics.EffectiveLoansPercentage;
        return (revenue, debt);
    }

    public static VerdictResult Compute(
        decimal? revenue,
        decimal? debt,
        decimal? investments = null,
        decimal? cash = null,
        bool? activityCompliant = null)
    {
        // Activity gate (kept): prohibited core business is standalone NonCompliant.
        if (activityCompliant == false)
        {
            var critA = BuildCriteria(revenue, debt, investments, cash);
            return new VerdictResult(
                ShariahStatus.NonCompliant, false, 0, 6,
                revenue, debt, investments, cash,
                MissingFor(revenue, debt), critA);
        }

        // Insufficient data: EITHER debt OR revenue missing. No evaluation.
        if (revenue == null || debt == null)
        {
            var crit = BuildCriteria(revenue, debt, investments, cash, markAllFailed: true);
            return new VerdictResult(
                ShariahStatus.Pending, false, 0, 6,
                revenue, debt, investments, cash,
                MissingFor(revenue, debt), crit);
        }

        var criteria = BuildCriteria(revenue, debt, investments, cash);
        var main = criteria.Take(5).ToList();

        // Fully passes ≥1 main standard → Compliant (not via scholars).
        var passingMain = main.Where(c => c.Passed).ToList();
        if (passingMain.Count > 0)
        {
            return new VerdictResult(
                ShariahStatus.Compliant, false, passingMain.Count + (ScholarsPasses(revenue, debt, investments, cash) ? 1 : 0), 6,
                revenue, debt, investments, cash,
                new List<string>(), criteria);
        }

        // Scholars-only: ≥1 main standard where every available non-debt ratio
        // passes, the only failure is debt, and debt ≤ 50.
        if (debt.Value <= ScholarsDebtMax && ExistsScholarsQualifyingStandard(revenue.Value, debt.Value, investments, cash))
        {
            // Rebuild scholars row as passing (combined rule).
            criteria = BuildCriteria(revenue, debt, investments, cash, forceScholarsPass: true);
            return new VerdictResult(
                ShariahStatus.Compliant, true, 1, 6,
                revenue, debt, investments, cash,
                new List<string>(), criteria);
        }

        return new VerdictResult(
            ShariahStatus.NonCompliant, false, 0, 6,
            revenue, debt, investments, cash,
            new List<string>(), criteria);
    }

    public static VerdictResult ComputeForStock(StockShariahMetrics? metrics, ShariahCompliance? compliance)
    {
        var (rev, debt) = EffectiveRatios(metrics, compliance?.Pct);
        return Compute(rev, debt, null, null, metrics?.EffectiveCoreActivityCompliant);
    }

    private static List<string> MissingFor(decimal? revenue, decimal? debt)
    {
        var missing = new List<string>();
        if (debt == null) missing.Add("debt");
        if (revenue == null) missing.Add("revenue");
        return missing;
    }

    private static bool RatioPasses(decimal? value, decimal max)
        => value == null || value.Value <= max;

    private static bool ExistsScholarsQualifyingStandard(decimal revenue, decimal debt, decimal? investments, decimal? cash)
    {
        foreach (var t in ShariahStandardsEvaluator.AllThresholds)
        {
            bool revOk = revenue <= t.ProhibitedRevenueMax;
            bool invOk = investments == null || investments.Value <= t.ProhibitedInvestmentsMax;
            bool cashOk = cash == null || cash.Value <= t.CashMax;
            bool debtFails = debt > t.DebtMax;
            if (revOk && invOk && cashOk && debtFails)
                return true;
        }
        return false;
    }

    private static bool ScholarsPasses(decimal? revenue, decimal? debt, decimal? investments, decimal? cash)
    {
        if (revenue == null || debt == null) return false;
        if (debt.Value > ScholarsDebtMax) return false;
        return ExistsScholarsQualifyingStandard(revenue.Value, debt.Value, investments, cash);
    }

    private static List<CriterionResult> BuildCriteria(
        decimal? revenue, decimal? debt, decimal? investments, decimal? cash,
        bool markAllFailed = false, bool forceScholarsPass = false)
    {
        var list = new List<CriterionResult>();
        var defs = new[]
        {
            ("egx33", "EGX 33", ShariahStandardsEvaluator.Egx33),
            ("dfm", "DFM", ShariahStandardsEvaluator.Dfm),
            ("aaoifi", "AAOIFI", ShariahStandardsEvaluator.Aaoifi),
            ("sp", "S&P Shariah", ShariahStandardsEvaluator.Sp),
            ("klsi", "KLSI", ShariahStandardsEvaluator.Klsi),
        };
        foreach (var (id, en, t) in defs)
        {
            var ratios = new List<RatioCheck>
            {
                Mk("prohibitedRevenue", revenue, t.ProhibitedRevenueMax),
                Mk("debt", debt, t.DebtMax),
                Mk("prohibitedInvestments", investments, t.ProhibitedInvestmentsMax),
                Mk("cash", cash, t.CashMax),
            };
            var available = ratios.Where(r => r.HasData).ToList();
            bool passed = !markAllFailed && available.Count > 0 && available.All(r => r.Passed);
            // Insufficient case: no evaluation — mark every main standard not passed.
            list.Add(new CriterionResult(id, en, passed, available.Count > 0, ratios));
        }

        // Scholars row: debt ≤ 50 with — for other ratios. Pass follows the
        // combined rule (debt ≤ 50 AND ≥1 main standard whose non-debt ratios pass).
        bool scholarsHasData = debt != null;
        bool scholarsPass;
        if (markAllFailed) scholarsPass = false;
        else if (forceScholarsPass) scholarsPass = true;
        else scholarsPass = ScholarsPasses(revenue, debt, investments, cash);
        var scholarsRatios = new List<RatioCheck>
        {
            new("prohibitedRevenue", null, 0, false, false, "skipped"),
            new("debt", debt, ScholarsDebtMax, scholarsHasData,
                scholarsHasData && debt!.Value <= ScholarsDebtMax,
                !scholarsHasData ? "skipped" : (debt!.Value <= ScholarsDebtMax ? "pass" : "fail")),
            new("prohibitedInvestments", null, 0, false, false, "skipped"),
            new("cash", null, 0, false, false, "skipped"),
        };
        list.Add(new CriterionResult("scholars", "Individual Scholars", scholarsPass, scholarsHasData, scholarsRatios));
        return list;
    }

    private static RatioCheck Mk(string key, decimal? value, decimal max)
    {
        if (value == null) return new RatioCheck(key, null, max, false, false, "skipped");
        bool p = value.Value <= max;
        return new RatioCheck(key, value, max, true, p, p ? "pass" : "fail");
    }

    // ── Storage helpers ──────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public sealed record StoredDetails(
        [property: JsonPropertyName("passedCount")] int PassedCount,
        [property: JsonPropertyName("totalCount")] int TotalCount,
        [property: JsonPropertyName("viaScholarsOnly")] bool ViaScholarsOnly,
        [property: JsonPropertyName("revenue")] decimal? Revenue,
        [property: JsonPropertyName("debt")] decimal? Debt,
        [property: JsonPropertyName("missing")] List<string> Missing,
        [property: JsonPropertyName("criteria")] List<CriterionResult> Criteria);

    public static string Serialize(VerdictResult r)
        => JsonSerializer.Serialize(new StoredDetails(r.PassedCount, r.TotalCount, r.ViaScholarsOnly, r.EffectiveRevenue, r.EffectiveDebt, r.MissingRatios, r.Criteria), JsonOptions);

    public static StoredDetails? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<StoredDetails>(json, JsonOptions); }
        catch { return null; }
    }

    /// <summary>
    /// Applies a computed HALAL verdict onto a compliance row (HalalStatus +
    /// ViaScholarsOnly + VerdictDetailsJson; the overall Status is left for
    /// <see cref="ApplyOverall"/>). Returns true when anything changed.
    /// Never throws — a failed recompute keeps the previous stored value.
    /// </summary>
    public static bool TryApplyHalal(ShariahCompliance compliance, VerdictResult verdict, ILogger? logger = null)
    {
        try
        {
            string json = Serialize(verdict);
            bool changed = compliance.HalalStatus != verdict.Status
                || compliance.ViaScholarsOnly != verdict.ViaScholarsOnly
                || compliance.VerdictDetailsJson != json;
            compliance.HalalStatus = verdict.Status;
            compliance.ViaScholarsOnly = verdict.ViaScholarsOnly;
            compliance.VerdictDetailsJson = json;
            compliance.UpdatedAt = DateTime.UtcNow;
            return changed;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Halal verdict recompute failed for stock {StockId}; keeping previous stored value.", compliance.StockId);
            return false;
        }
    }

    /// <summary>
    /// Overall internal verdict ("الحكم الداخلي المعتمد"): restored aggregation
    /// across all independent sources, with the Halal Bourse input taken from the
    /// STORED ratio verdict (raw source values never count, doubtful never counts).
    /// Order (mirrors the pre-refactor resolver, Halal input swapped to stored):
    /// activity gate → other-board compliant → Halal stored compliant → EGX33 →
    /// Halal stored non-compliant → refusal gap-fill (no stored row only) →
    /// Halal insufficient → stored fallback → unknown.
    /// Halal Insufficient excludes Halal: other sources decide; with no compliant
    /// other source the result is Insufficient (Pending), never NonCompliant —
    /// bodies alone cannot condemn when ratios are missing.
    /// </summary>
    public static (ShariahStatus? Status, string Source) ComputeOverall(
        ShariahStatus? halalStatus,
        bool halalViaScholars,
        IEnumerable<string?> otherOpinionStatuses,
        bool isShariahIndexMember,
        bool? activityCompliant,
        ShariahStatus? storedOverall,
        bool hasStoredRow)
    {
        // 1. Activity screen wins (effective value: override wins over feed).
        if (activityCompliant == false)
            return (ShariahStatus.NonCompliant, OverallSource.Activity);

        var sanitized = (otherOpinionStatuses ?? Enumerable.Empty<string?>())
            .Select(SanitizeOpinionStatus)
            .ToList();
        bool otherCompliant = sanitized.Any(s => s != null && s.ToUpperInvariant() == "COMPLIANT");
        bool refused = sanitized.Any(s => NormalizeOpinion(s) == "NONCOMPLIANT");
        bool unresolved = sanitized.Any(s => NormalizeOpinion(s) == "PENDING" || NormalizeOpinion(s) == "BLOCKED");
        bool halalInsufficient = halalStatus == ShariahStatus.Pending;

        // 2. Any other (non-Halal) board says compliant → Compliant.
        if (otherCompliant)
            return (ShariahStatus.Compliant, halalInsufficient ? OverallSource.BoardsFallback : OverallSource.Boards);

        // 2b. Halal stored compliant (incl. via scholars) → Compliant.
        if (halalStatus == ShariahStatus.Compliant)
            return (ShariahStatus.Compliant, halalViaScholars ? OverallSource.HalalScholars : OverallSource.Halal);

        // 2c. EGX33 Shariah-index membership: same weight as a board opinion.
        if (isShariahIndexMember)
            return (ShariahStatus.Compliant, halalInsufficient ? OverallSource.BoardsFallback : OverallSource.EGX33);

        // Halal stored non-compliant → ratios condemn (no seed fallback).
        if (halalStatus == ShariahStatus.NonCompliant)
            return (ShariahStatus.NonCompliant, OverallSource.HalalNonCompliant);

        // 2d. Explicit refusal with no counter-signal and no stored row.
        if (!hasStoredRow && refused && !unresolved)
            return (ShariahStatus.NonCompliant, OverallSource.GapFill);

        // Halal insufficient (and nobody vouches) → Insufficient, never NonCompliant.
        if (halalInsufficient)
            return (ShariahStatus.Pending, OverallSource.Insufficient);

        // 3. Stored overall fallback (only if no board says compliant).
        if (storedOverall != null)
            return (storedOverall, OverallSource.Stored);

        // 4. No data.
        return (null, OverallSource.Unknown);
    }

    private static string NormalizeOpinion(string? status)
        => (status ?? string.Empty).Trim().ToUpperInvariant()
            .Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);

    /// <summary>
    /// Applies the overall verdict onto a compliance row (Status + VerdictSource),
    /// reading the Halal input from the row's own stored Halal fields. Call only
    /// for affected stocks, in the same operation that changed the inputs.
    /// Never throws — keeps the previous stored value on failure.
    /// </summary>
    public static bool ApplyOverall(
        ShariahCompliance compliance,
        bool? activityCompliant,
        IEnumerable<ShariahSourceOpinion>? opinions,
        bool isShariahIndexMember,
        ILogger? logger = null)
    {
        try
        {
            var others = (opinions ?? Enumerable.Empty<ShariahSourceOpinion>())
                .Where(o => o.SourceKey != ShariahSourceKey.HalalBourse)
                .Select(o => o.Status);
            var (status, source) = ComputeOverall(
                compliance.HalalStatus,
                compliance.ViaScholarsOnly,
                others,
                isShariahIndexMember,
                activityCompliant,
                compliance.Status,
                hasStoredRow: true);
            bool changed = compliance.Status != status || compliance.VerdictSource != source;
            if (status.HasValue) compliance.Status = status.Value;
            compliance.VerdictSource = source;
            compliance.UpdatedAt = DateTime.UtcNow;
            return changed;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Overall verdict recompute failed for stock {StockId}; keeping previous stored value.", compliance.StockId);
            return false;
        }
    }

    /// <summary>
    /// True when the Halal ratio inputs changed (effective revenue/debt/activity).
    /// </summary>
    public static bool HalalInputsChanged(
        decimal? oldRevenue, decimal? oldDebt, bool? oldActivity,
        decimal? newRevenue, decimal? newDebt, bool? newActivity)
        => oldRevenue != newRevenue || oldDebt != newDebt || oldActivity != newActivity;

    /// <summary>
    /// True when a NON-Halal board's vote changed (sanitized status or percentage).
    /// Raw Halal changes alone never trigger an overall recompute.
    /// </summary>
    public static bool OpinionInputsChanged(
        IReadOnlyDictionary<ShariahSourceKey, (string? Status, decimal? Percentage)> before,
        IReadOnlyDictionary<ShariahSourceKey, (string? Status, decimal? Percentage)> after)
    {
        var keys = before.Keys.Concat(after.Keys)
            .Where(k => k != ShariahSourceKey.HalalBourse)
            .Distinct()
            .ToList();
        foreach (var k in keys)
        {
            before.TryGetValue(k, out var b);
            after.TryGetValue(k, out var a);
            if (SanitizeOpinionStatus(b.Status) != SanitizeOpinionStatus(a.Status)) return true;
            if (b.Percentage != a.Percentage) return true;
        }
        return false;
    }

    /// <summary>
    /// Doubtful/مشكوك from any source must never reach the UI: map to null
    /// (no opinion) so it is excluded from cards, counters and filters.
    /// </summary>
    public static bool IsDoubtfulLike(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var s = status.Trim();
        if (s.Contains("مشكوك") || s.Contains("مشكوك")) return true;
        var n = s.ToUpperInvariant().Replace("-", "").Replace("_", "").Replace(" ", "");
        return n == "DOUBTFUL";
    }

    public static string? SanitizeOpinionStatus(string? status)
        => IsDoubtfulLike(status) ? null : status;
}
