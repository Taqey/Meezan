using Meezan.Application.Common;
using Meezan.Application.Common.Services.Text;
using Meezan.Application.Common.Services.Market;
using Meezan.Application.Common.Services.Shariah;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Infrastructure.Tests;

/// <summary>
/// Covers ShariahStandardsEvaluator thresholds and their integration into
/// ShariahStatusResolver.EffectiveStatus, where the Halal Bourse input is the
/// STORED ratio verdict (raw doubtful/مشكوك never counts).
/// </summary>
public class ShariahStandardsEvaluatorTests
{
    // ── PassesAnyStandard ─────────────────────────────────────────────

    [Fact]
    public void AbukWorkedExample_PassesThreeOfFive()
    {
        // ABUK: prohibited revenue 8.5%, loans 0%, investments and cash missing.
        // EGX33 (10), DFM (10) and KLSI (20) pass; AAOIFI (5) and S&P (5) fail.
        Assert.True(ShariahStandardsEvaluator.PassesAnyStandard(8.5m, 0m, null, null));
    }

    [Fact]
    public void AllRatiosFailingEveryStandard_ReturnsFalse()
    {
        Assert.False(ShariahStandardsEvaluator.PassesAnyStandard(60m, 80m, 80m, 90m));
    }

    [Fact]
    public void NoDataAtAll_ReturnsFalse()
    {
        Assert.False(ShariahStandardsEvaluator.PassesAnyStandard(null, null, null, null));
    }

    [Theory]
    [InlineData(10, 33)] // EGX33 boundary, inclusive
    [InlineData(5, 30)]  // AAOIFI boundary, inclusive
    [InlineData(20, 33)] // KLSI boundary, inclusive
    public void ExactlyAtThreshold_Passes(decimal revenue, decimal debt)
    {
        Assert.True(ShariahStandardsEvaluator.PassesAnyStandard(revenue, debt, 0m, 0m));
    }

    [Fact]
    public void JustAboveThreshold_FailsThatStandard()
    {
        // 10.01% revenue fails EGX33/DFM (10) and AAOIFI/S&P (5); KLSI (20) still passes.
        Assert.True(ShariahStandardsEvaluator.PassesAnyStandard(10.01m, 0m, 0m, 0m));
        // ...but with debt also breaching KLSI, nothing passes.
        Assert.False(ShariahStandardsEvaluator.PassesAnyStandard(10.01m, 40m, 40m, 80m));
    }

    [Fact]
    public void MissingCriteriaExcluded_NeverTreatedAsZero()
    {
        // Revenue missing, debt 40% fails every debt ceiling (30/33):
        // must fail even though a "0" revenue would pass.
        Assert.False(ShariahStandardsEvaluator.PassesAnyStandard(null, 40m, null, null));
        // Revenue missing, debt fine: passes on the available criterion alone.
        Assert.True(ShariahStandardsEvaluator.PassesAnyStandard(null, 5m, null, null));
    }

    // ── IsDoubtfulBourseHalalOpinion ──────────────────────────────────

    [Theory]
    [InlineData("Doubtful", true)]
    [InlineData("doubtful", true)]
    [InlineData("Compliant", true)]  // missing percentage → doubtful
    [InlineData("NonCompliant", false)]
    [InlineData(null, false)]
    public void DoubtfulDetection_MatchesFrontendCardRule(string? status, bool expected)
    {
        Assert.Equal(expected, ShariahStandardsEvaluator.IsDoubtfulBourseHalalOpinion(status, null));
    }

    [Theory]
    [InlineData("doubtful", 100.0, true)]
    [InlineData("Compliant", 50.0, true)]  // percentage < 100 → doubtful
    [InlineData("Compliant", 99.99, true)]
    [InlineData("Compliant", 100.0, false)] // full 100 → genuinely compliant
    [InlineData("NonCompliant", 100.0, false)]
    public void DoubtfulDetection_WithPercentage(string? status, double pct, bool expected)
    {
        Assert.Equal(expected, ShariahStandardsEvaluator.IsDoubtfulBourseHalalOpinion(status, (decimal)pct));
    }

    // ── EffectiveStatus integration ───────────────────────────────────

    private static (ShariahCompliance Compliance, StockShariahMetrics Metrics, List<ShariahSourceOpinion> Opinions) AbukLike(
        string hbStatus = "Compliant", decimal? hbPct = 50m,
        decimal? revenue = 8.5m, decimal? debt = 0m,
        ShariahStatus? halalStatus = ShariahStatus.Compliant, bool viaScholars = false)
    {
        var compliance = new ShariahCompliance
        {
            StockId = 1,
            Status = ShariahStatus.NonCompliant,
            HalalStatus = halalStatus,
            ViaScholarsOnly = viaScholars,
            VerdictSource = halalStatus == ShariahStatus.Compliant
                ? (viaScholars ? ShariahVerdictService.OverallSource.HalalScholars : ShariahVerdictService.OverallSource.Halal)
                : null
        };
        var metrics = new StockShariahMetrics
        {
            StockId = 1,
            SpHaramEarningPercentage = revenue,
            LoansPercentage = debt,
            CoreActivityCompliant = true
        };
        var opinions = new List<ShariahSourceOpinion>
        {
            new() { StockId = 1, SourceKey = ShariahSourceKey.HalalBourse, Status = hbStatus, Percentage = hbPct },
            new() { StockId = 1, SourceKey = ShariahSourceKey.Musaffa, Status = "NonCompliant" }
        };
        return (compliance, metrics, opinions);
    }

    [Fact]
    public void DoubtfulBourseHalal_PassingStandards_UpgradesToCompliant()
    {
        // Stored Halal verdict (ratios 8.5%/0% pass) decides; the raw doubtful
        // value never counts. Same expectation as pre-refactor, new input.
        var (c, m, o) = AbukLike();
        Assert.Equal(ShariahStatus.Compliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    [Fact]
    public void DoubtfulBourseHalal_FailingAllStandards_KeepsStoredVerdict()
    {
        var (c, m, o) = AbukLike(revenue: 60m, debt: 80m, halalStatus: ShariahStatus.NonCompliant);
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    [Fact]
    public void AlreadyCompliantBourseHalal_BehavesAsBefore()
    {
        // A raw full-percentage "compliant" alone never counts: the stored Halal
        // verdict (NonCompliant here — ratios 60%/80% fail) decides instead.
        var (c, m, o) = AbukLike(hbStatus: "Compliant", hbPct: 100m, revenue: 60m, debt: 80m, halalStatus: ShariahStatus.NonCompliant);
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    [Fact]
    public void NoBourseHalalOpinion_NoUpgrade()
    {
        // No Halal opinion row and stored Halal NonCompliant: nothing vouches.
        var (c, m, o) = AbukLike(halalStatus: ShariahStatus.NonCompliant);
        o.RemoveAll(x => x.SourceKey == ShariahSourceKey.HalalBourse);
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    [Fact]
    public void ActivityGate_StillWinsOverUpgrade()
    {
        var (c, m, o) = AbukLike();
        m.CoreActivityCompliant = false;
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    [Fact]
    public void AllRed_NoData_StaysNonCompliant()
    {
        var c = new ShariahCompliance { StockId = 2, Status = ShariahStatus.NonCompliant };
        var m = new StockShariahMetrics { StockId = 2, CoreActivityCompliant = true };
        var o = new List<ShariahSourceOpinion>
        {
            new() { StockId = 2, SourceKey = ShariahSourceKey.HalalBourse, Status = "NonCompliant" },
            new() { StockId = 2, SourceKey = ShariahSourceKey.Musaffa, Status = "NonCompliant" }
        };
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    // ── EGX33 membership (FAITA case) ─────────────────────────────────

    [Fact]
    public void Egx33Member_WithNoRows_ResolvesCompliant()
    {
        // FAITA shape: no compliance row, no opinions, no metrics — but the
        // stock is listed in the EGX33 Shariah index.
        Assert.Equal(
            ShariahStatus.Compliant,
            ShariahStatusResolver.EffectiveStatus(null, null, new List<ShariahSourceOpinion>(), isShariahIndexMember: true));
    }

    [Theory]
    [InlineData("Shariah")]
    [InlineData("EGX33")]
    [InlineData("EGX 33")]
    [InlineData(" egx 33 ")]
    public void ShariahIndexCodes_RecognizedInAllSpellings(string code)
    {
        Assert.True(ShariahStatusResolver.IsShariahIndexCode(code));
    }

    [Theory]
    [InlineData("EGX30")]
    [InlineData("Sectoral-Indices")]
    [InlineData(null)]
    [InlineData("")]
    public void NonShariahIndexCodes_Rejected(string? code)
    {
        Assert.False(ShariahStatusResolver.IsShariahIndexCode(code));
    }

    [Fact]
    public void Egx33Member_WithActivityGate_StaysNonCompliant()
    {
        var m = new StockShariahMetrics { StockId = 3, CoreActivityCompliant = false };
        Assert.Equal(
            ShariahStatus.NonCompliant,
            ShariahStatusResolver.EffectiveStatus(null, m, new List<ShariahSourceOpinion>(), isShariahIndexMember: true));
    }

    [Fact]
    public void NonMember_WithoutRows_StaysNull()
    {
        // CPME shape: genuinely no data anywhere — honestly unknown.
        Assert.Null(ShariahStatusResolver.EffectiveStatus(null, null, new List<ShariahSourceOpinion>()));
    }

    // ── Explicit-refusal gap fill (EKHO/VLMR shape) ────────────────────

    [Fact]
    public void AllRefused_NoStoredRow_ResolvesNonCompliant()
    {
        var m = new StockShariahMetrics { StockId = 4, CoreActivityCompliant = true };
        var o = new List<ShariahSourceOpinion>
        {
            new() { StockId = 4, SourceKey = ShariahSourceKey.HalalBourse, Status = "doubtful", Percentage = 83 },
            new() { StockId = 4, SourceKey = ShariahSourceKey.Musaffa, Status = "non_compliant" },
            new() { StockId = 4, SourceKey = ShariahSourceKey.Kashif, Status = "non_compliant" }
        };
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(null, m, o));
    }

    [Fact]
    public void PendingOpinion_BlocksNegativeGapFill()
    {
        var m = new StockShariahMetrics { StockId = 5, CoreActivityCompliant = true };
        var o = new List<ShariahSourceOpinion>
        {
            new() { StockId = 5, SourceKey = ShariahSourceKey.Musaffa, Status = "non_compliant" },
            new() { StockId = 5, SourceKey = ShariahSourceKey.Thndr, Status = "Pending" }
        };
        Assert.Null(ShariahStatusResolver.EffectiveStatus(null, m, o));
    }

    [Fact]
    public void StoredVerdict_NeverOverriddenByRefusals()
    {
        var c = new ShariahCompliance { StockId = 6, Status = ShariahStatus.Compliant };
        var m = new StockShariahMetrics { StockId = 6, CoreActivityCompliant = true };
        var o = new List<ShariahSourceOpinion>
        {
            new() { StockId = 6, SourceKey = ShariahSourceKey.Musaffa, Status = "non_compliant" }
        };
        Assert.Equal(ShariahStatus.Compliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    [Theory]
    [InlineData("non_compliant", true)]
    [InlineData("non-compliant", true)]
    [InlineData("NONCOMPLIANT", true)]
    [InlineData("doubtful", false)]
    [InlineData("not_available", false)]
    [InlineData(null, false)]
    public void OpinionRefusalNormalization(string? status, bool expectedRefusal)
    {
        var norm = ShariahStatusResolver.NormalizeOpinionStatus(status);
        Assert.Equal(expectedRefusal, norm == "NONCOMPLIANT");
    }
}
