using Meezan.Application.Common.Services.Shariah;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Infrastructure.Tests.Features.Shariah;

/// <summary>
/// Covers the ONE shared verdict rule: scholars only relaxes debt, all other
/// ratios must still pass; debt OR revenue missing → Insufficient (Pending);
/// doubtful never surfaces; committee opinions never change the verdict.
/// </summary>
public class ShariahVerdictServiceTests
{
    [Fact]
    public void Fwry_Revenue33_Debt17_NonCompliant()
    {
        // FWRY: revenue 33% exceeds every main standard's max (highest 20 KLSI),
        // debt is not the failing ratio → Non-compliant even though debt ≤ 50%.
        var v = ShariahVerdictService.Compute(33m, 17.1m, null, null, true);
        Assert.Equal(ShariahStatus.NonCompliant, v.Status);
        Assert.False(v.ViaScholarsOnly);
    }

    [Fact]
    public void Miph_Revenue089_Debt34_CompliantViaScholars()
    {
        // MIPH: revenue 0.89% passes everywhere; debt 34.12% fails every debt max
        // (30/33) but ≤ 50 → Compliant via scholars only.
        var v = ShariahVerdictService.Compute(0.89m, 34.12m, null, null, true);
        Assert.Equal(ShariahStatus.Compliant, v.Status);
        Assert.True(v.ViaScholarsOnly);
        Assert.Equal(1, v.PassedCount);
        Assert.Equal(6, v.TotalCount);
        var scholars = v.Criteria.First(c => c.Id == "scholars");
        Assert.True(scholars.Passed);
    }

    [Fact]
    public void DebtAvailable_RevenueMissing_Insufficient()
    {
        var v = ShariahVerdictService.Compute(null, 10m, null, null, true);
        Assert.Equal(ShariahStatus.Pending, v.Status);
        Assert.False(v.ViaScholarsOnly);
        Assert.Contains("revenue", v.MissingRatios);
    }

    [Fact]
    public void RevenueAvailable_DebtMissing_Insufficient()
    {
        var v = ShariahVerdictService.Compute(5m, null, null, null, true);
        Assert.Equal(ShariahStatus.Pending, v.Status);
        Assert.Contains("debt", v.MissingRatios);
    }

    [Fact]
    public void BothMissing_Insufficient()
    {
        var v = ShariahVerdictService.Compute(null, null, null, null, true);
        Assert.Equal(ShariahStatus.Pending, v.Status);
    }

    [Fact]
    public void InvestmentsCashMissing_VerdictComputedNormally()
    {
        // Only revenue+debt available → normal evaluation (here fully passes).
        var v = ShariahVerdictService.Compute(4m, 10m, null, null, true);
        Assert.Equal(ShariahStatus.Compliant, v.Status);
        Assert.False(v.ViaScholarsOnly);
    }

    [Fact]
    public void Insufficient_WithScholarsDebtLow_StillInsufficient()
    {
        // Debt ≤ 50% never rescues missing revenue.
        var v = ShariahVerdictService.Compute(null, 10m, null, null, true);
        Assert.Equal(ShariahStatus.Pending, v.Status);
    }

    [Fact]
    public void RevenueAboveEveryMax_DebtLow_NonCompliant()
    {
        var v = ShariahVerdictService.Compute(33m, 10m, null, null, true);
        Assert.Equal(ShariahStatus.NonCompliant, v.Status);
        Assert.False(v.ViaScholarsOnly);
    }

    [Fact]
    public void DebtAbove50_NonCompliant()
    {
        var v = ShariahVerdictService.Compute(2m, 51m, null, null, true);
        Assert.Equal(ShariahStatus.NonCompliant, v.Status);
        Assert.False(v.ViaScholarsOnly);
    }

    [Fact]
    public void FullyPassesMainStandard_CompliantNoScholarsNote()
    {
        var v = ShariahVerdictService.Compute(4m, 10m, 10m, 20m, true);
        Assert.Equal(ShariahStatus.Compliant, v.Status);
        Assert.False(v.ViaScholarsOnly);
    }

    [Fact]
    public void ActivityFalse_ForcesNonCompliant()
    {
        var v = ShariahVerdictService.Compute(1m, 1m, null, null, false);
        Assert.Equal(ShariahStatus.NonCompliant, v.Status);
    }

    [Fact]
    public void DoubtfulSanitizedToNull()
    {
        Assert.Null(ShariahVerdictService.SanitizeOpinionStatus("doubtful"));
        Assert.Null(ShariahVerdictService.SanitizeOpinionStatus("Doubtful"));
        Assert.Null(ShariahVerdictService.SanitizeOpinionStatus("مشكوك"));
        Assert.Equal("Compliant", ShariahVerdictService.SanitizeOpinionStatus("Compliant"));
        Assert.Equal("NonCompliant", ShariahVerdictService.SanitizeOpinionStatus("NonCompliant"));
    }

    [Fact]
    public void EffectiveRatios_OverrideWinsOverFeed()
    {
        var m = new StockShariahMetrics
        {
            StockId = 1,
            HaramEarningsPercentage = 33m,
            LoansPercentage = 40m,
            HaramRevenuePercentageOverride = 2m,
            LoansPercentageOverride = 10m,
            CoreActivityCompliant = true
        };
        var v = ShariahVerdictService.ComputeForStock(m, null);
        Assert.Equal(2m, v.EffectiveRevenue);
        Assert.Equal(10m, v.EffectiveDebt);
        Assert.Equal(ShariahStatus.Compliant, v.Status);
    }

    [Fact]
    public void ManualEdit_FlipsVerdict()
    {
        // Compliant → Non-compliant and back via effective ratios.
        var ok = ShariahVerdictService.Compute(2m, 10m, null, null, true);
        Assert.Equal(ShariahStatus.Compliant, ok.Status);
        var bad = ShariahVerdictService.Compute(33m, 10m, null, null, true);
        Assert.Equal(ShariahStatus.NonCompliant, bad.Status);
        var okAgain = ShariahVerdictService.Compute(2m, 10m, null, null, true);
        Assert.Equal(ShariahStatus.Compliant, okAgain.Status);
    }

    [Fact]
    public void Thresholds_SingleSource_NoDuplicates()
    {
        // Backend thresholds must match the frontend SHARIAH_STANDARDS (10/33, 10/30, 5/30, 5/33, 20/33).
        var all = ShariahStandardsEvaluator.AllThresholds;
        Assert.Equal(5, all.Count);
        Assert.Equal((10m, 33m), (all[0].ProhibitedRevenueMax, all[0].DebtMax));
        Assert.Equal((10m, 30m), (all[1].ProhibitedRevenueMax, all[1].DebtMax));
        Assert.Equal((5m, 30m), (all[2].ProhibitedRevenueMax, all[2].DebtMax));
        Assert.Equal((5m, 33m), (all[3].ProhibitedRevenueMax, all[3].DebtMax));
        Assert.Equal((20m, 33m), (all[4].ProhibitedRevenueMax, all[4].DebtMax));
    }

    [Fact]
    public void StoredDetails_RoundTrips()
    {
        var v = ShariahVerdictService.Compute(0.89m, 34.12m, null, null, true);
        var json = ShariahVerdictService.Serialize(v);
        Assert.False(string.IsNullOrWhiteSpace(json));
        var back = ShariahVerdictService.Deserialize(json);
        Assert.NotNull(back);
        Assert.Equal(1, back!.PassedCount);
        Assert.Equal(6, back.TotalCount);
        Assert.True(back.ViaScholarsOnly);
        Assert.Equal(6, back.Criteria.Count);
    }

    [Fact]
    public void SufficientRatios_NoRawHalalOpinion_StillComputesVerdictAndDto()
    {
        // When a stock has sufficient ratios (revenue + debt) and no raw Halal Bourse opinion,
        // it still gets a computed Halal verdict result and serialization.
        var metrics = new StockShariahMetrics
        {
            StockId = 999,
            SpHaramEarningPercentage = 4m,
            LoansPercentage = 25m,
            CoreActivityCompliant = true
        };
        var compliance = new ShariahCompliance
        {
            StockId = 999
        };

        var verdict = ShariahVerdictService.ComputeForStock(metrics, compliance);
        Assert.Equal(ShariahStatus.Compliant, verdict.Status);
        Assert.False(verdict.ViaScholarsOnly);
        Assert.True(verdict.PassedCount > 0);

        var json = ShariahVerdictService.Serialize(verdict);
        Assert.NotNull(json);
        Assert.Contains("\"passedCount\"", json);

        var (overallStatus, source) = ShariahVerdictService.ComputeOverall(
            verdict.Status, verdict.ViaScholarsOnly,
            otherOpinionStatuses: null!,
            isShariahIndexMember: false,
            activityCompliant: true,
            storedOverall: null,
            hasStoredRow: false);

        Assert.Equal(ShariahStatus.Compliant, overallStatus);
        Assert.Equal(ShariahVerdictService.OverallSource.Halal, source);
    }

    [Fact]
    public void Fwry_Inputs_ThroughFullPath_ReturnsNonCompliant()
    {
        var metrics = new StockShariahMetrics
        {
            StockId = 464,
            SpHaramEarningPercentage = 33m,
            LoansPercentage = 17.1m,
            CoreActivityCompliant = true
        };
        var compliance = new ShariahCompliance
        {
            StockId = 464,
            Status = ShariahStatus.NonCompliant
        };

        var halal = ShariahVerdictService.ComputeForStock(metrics, compliance);
        Assert.Equal(ShariahStatus.NonCompliant, halal.Status);
        Assert.False(halal.ViaScholarsOnly);
        Assert.Equal(0, halal.PassedCount);

        var opinions = new List<string> { "non_compliant", "non_compliant", "non_compliant" };
        var (overallStatus, source) = ShariahVerdictService.ComputeOverall(
            halal.Status, halal.ViaScholarsOnly,
            opinions,
            isShariahIndexMember: false,
            activityCompliant: true,
            storedOverall: ShariahStatus.NonCompliant,
            hasStoredRow: true);

        Assert.Equal(ShariahStatus.NonCompliant, overallStatus);
        Assert.Equal(ShariahVerdictService.OverallSource.HalalNonCompliant, source);
    }
}
