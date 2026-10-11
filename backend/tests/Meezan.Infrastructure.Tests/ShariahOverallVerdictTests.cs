using Meezan.Application.Common;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Infrastructure.Tests;

/// <summary>
/// Overall internal verdict: restored board/EGX33/gap-fill aggregation where the
/// Halal Bourse input is the STORED ratio verdict (HalalStatus), never the raw
/// source value. Halal Insufficient (Pending) excludes Halal from the aggregation.
/// </summary>
public class ShariahOverallVerdictTests
{
    private static ShariahCompliance Row(
        int stockId,
        ShariahStatus status,
        ShariahStatus? halal,
        bool viaScholars = false,
        string? source = null)
        => new()
        {
            StockId = stockId,
            Status = status,
            HalalStatus = halal,
            ViaScholarsOnly = viaScholars,
            VerdictSource = source
        };

    private static StockShariahMetrics Metrics(int stockId, bool? activity = true)
        => new() { StockId = stockId, CoreActivityCompliant = activity };

    private static ShariahSourceOpinion Op(int stockId, ShariahSourceKey key, string? status)
        => new() { StockId = stockId, SourceKey = key, Status = status };

    // ── Task cases ────────────────────────────────────────────────────

    [Fact]
    public void RatiosSufficientPassing_OthersNonCompliant_CompliantViaHalal()
    {
        // Halal stored Compliant participates exactly like the old aggregation:
        // it wins over the other bodies' refusals.
        var c = Row(1, ShariahStatus.NonCompliant, ShariahStatus.Compliant);
        var m = Metrics(1);
        var o = new List<ShariahSourceOpinion>
        {
            Op(1, ShariahSourceKey.HalalBourse, "NonCompliant"),
            Op(1, ShariahSourceKey.Musaffa, "non_compliant"),
            Op(1, ShariahSourceKey.Kashif, "non_compliant"),
        };
        Assert.Equal(ShariahStatus.Compliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
        Assert.Equal(ShariahVerdictService.OverallSource.Halal, ShariahStatusResolver.EffectiveSource(c, m, o));
    }

    [Fact]
    public void RatiosInsufficient_OtherCompliant_CompliantViaFallback()
    {
        // Halal Insufficient is excluded; Musaffa's compliant decides → Compliant
        // with the BoardsFallback path (header fallback note).
        var c = Row(2, ShariahStatus.Pending, ShariahStatus.Pending);
        var m = Metrics(2);
        var o = new List<ShariahSourceOpinion>
        {
            Op(2, ShariahSourceKey.Musaffa, "Compliant"),
            Op(2, ShariahSourceKey.Kashif, "non_compliant"),
        };
        Assert.Equal(ShariahStatus.Compliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
        Assert.Equal(ShariahVerdictService.OverallSource.BoardsFallback, ShariahStatusResolver.EffectiveSource(c, m, o));
    }

    [Fact]
    public void RatiosInsufficient_AllOthersNonCompliant_Insufficient()
    {
        // Bodies alone cannot condemn when ratios are missing → Insufficient, not NC.
        var c = Row(3, ShariahStatus.Pending, ShariahStatus.Pending);
        var m = Metrics(3);
        var o = new List<ShariahSourceOpinion>
        {
            Op(3, ShariahSourceKey.Musaffa, "non_compliant"),
            Op(3, ShariahSourceKey.Kashif, "non_compliant"),
        };
        Assert.Equal(ShariahStatus.Pending, ShariahStatusResolver.EffectiveStatus(c, m, o));
        Assert.Equal(ShariahVerdictService.OverallSource.Insufficient, ShariahStatusResolver.EffectiveSource(c, m, o));
    }

    [Fact]
    public void RatiosInsufficient_NoOpinions_Insufficient()
    {
        var c = Row(4, ShariahStatus.Pending, ShariahStatus.Pending);
        var m = Metrics(4);
        Assert.Equal(ShariahStatus.Pending, ShariahStatusResolver.EffectiveStatus(c, m, new List<ShariahSourceOpinion>()));
    }

    [Fact]
    public void RawHalalCompliantAlone_NeverCounts()
    {
        // Halal raw "Compliant" 100% with stored Halal NonCompliant and no other
        // compliant body → NonCompliant. The raw value alone never flips a verdict.
        var c = Row(5, ShariahStatus.NonCompliant, ShariahStatus.NonCompliant);
        var m = Metrics(5);
        var o = new List<ShariahSourceOpinion>
        {
            Op(5, ShariahSourceKey.HalalBourse, "Compliant"),
        };
        o[0].Percentage = 100m;
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    [Fact]
    public void ActivityGateFalse_ForcesNonCompliant()
    {
        var c = Row(6, ShariahStatus.Compliant, ShariahStatus.Compliant);
        var m = Metrics(6, activity: false);
        var o = new List<ShariahSourceOpinion> { Op(6, ShariahSourceKey.Musaffa, "Compliant") };
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
        Assert.Equal(ShariahVerdictService.OverallSource.Activity, ShariahStatusResolver.EffectiveSource(c, m, o));
    }

    // ── FWRY / MIPH Halal results unchanged ───────────────────────────

    [Fact]
    public void Fwry_Halal_NonCompliant_Overall_NonCompliant()
    {
        var halal = ShariahVerdictService.Compute(33m, 17.1m, null, null, true);
        Assert.Equal(ShariahStatus.NonCompliant, halal.Status);
        var c = Row(7, ShariahStatus.NonCompliant, halal.Status);
        var m = Metrics(7);
        var o = new List<ShariahSourceOpinion> { Op(7, ShariahSourceKey.Musaffa, "non_compliant") };
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
    }

    [Fact]
    public void Miph_Halal_Scholars_Overall_CompliantHalalScholars()
    {
        var halal = ShariahVerdictService.Compute(0.89m, 34.12m, null, null, true);
        Assert.Equal(ShariahStatus.Compliant, halal.Status);
        Assert.True(halal.ViaScholarsOnly);
        var c = Row(8, ShariahStatus.Pending, halal.Status, viaScholars: true);
        var m = Metrics(8);
        var o = new List<ShariahSourceOpinion> { Op(8, ShariahSourceKey.Musaffa, "non_compliant") };
        Assert.Equal(ShariahStatus.Compliant, ShariahStatusResolver.EffectiveStatus(c, m, o));
        Assert.Equal(ShariahVerdictService.OverallSource.HalalScholars, ShariahStatusResolver.EffectiveSource(c, m, o));
    }

    // ── Membership flips ──────────────────────────────────────────────

    [Fact]
    public void Egx33Membership_FlipsInsufficientToFallbackCompliant()
    {
        var c = Row(9, ShariahStatus.Pending, ShariahStatus.Pending);
        var m = Metrics(9);
        var none = new List<ShariahSourceOpinion>();
        Assert.Equal(ShariahStatus.Pending, ShariahStatusResolver.EffectiveStatus(c, m, none, isShariahIndexMember: false));
        Assert.Equal(ShariahStatus.Compliant, ShariahStatusResolver.EffectiveStatus(c, m, none, isShariahIndexMember: true));
        Assert.Equal(ShariahVerdictService.OverallSource.BoardsFallback, ShariahStatusResolver.EffectiveSource(c, m, none, isShariahIndexMember: true));
    }

    [Fact]
    public void Egx33Removal_FlipsBackToStored()
    {
        // Halal unknown (stale row), stored NonCompliant: member → Compliant,
        // removed → stored NonCompliant again.
        var c = Row(10, ShariahStatus.NonCompliant, halal: null);
        var m = Metrics(10);
        var none = new List<ShariahSourceOpinion>();
        Assert.Equal(ShariahStatus.Compliant, ShariahStatusResolver.EffectiveStatus(c, m, none, isShariahIndexMember: true));
        Assert.Equal(ShariahStatus.NonCompliant, ShariahStatusResolver.EffectiveStatus(c, m, none, isShariahIndexMember: false));
    }

    // ── Recompute trigger decisions ───────────────────────────────────

    [Fact]
    public void OpinionChange_TriggersOverallRecompute()
    {
        var before = new Dictionary<ShariahSourceKey, (string? Status, decimal? Percentage)>
        {
            [ShariahSourceKey.Musaffa] = ("non_compliant", null),
            [ShariahSourceKey.HalalBourse] = ("Compliant", 100m),
        };
        var afterStatusFlip = new Dictionary<ShariahSourceKey, (string? Status, decimal? Percentage)>
        {
            [ShariahSourceKey.Musaffa] = ("Compliant", null),
            [ShariahSourceKey.HalalBourse] = ("Compliant", 100m),
        };
        Assert.True(ShariahVerdictService.OpinionInputsChanged(before, afterStatusFlip));

        // Raw Halal change alone (even to doubtful) never triggers it.
        var afterHalalOnly = new Dictionary<ShariahSourceKey, (string? Status, decimal? Percentage)>
        {
            [ShariahSourceKey.Musaffa] = ("non_compliant", null),
            [ShariahSourceKey.HalalBourse] = ("doubtful", null),
        };
        Assert.False(ShariahVerdictService.OpinionInputsChanged(before, afterHalalOnly));

        // Note-only edits do not change votes.
        Assert.False(ShariahVerdictService.OpinionInputsChanged(before, before));
    }

    [Fact]
    public void HalalInputsChange_Detected()
    {
        Assert.True(ShariahVerdictService.HalalInputsChanged(8.5m, 0m, true, 33m, 0m, true));
        Assert.True(ShariahVerdictService.HalalInputsChanged(8.5m, 0m, true, 8.5m, 0m, false));
        Assert.False(ShariahVerdictService.HalalInputsChanged(8.5m, 0m, true, 8.5m, 0m, true));
        Assert.False(ShariahVerdictService.HalalInputsChanged(null, null, null, null, null, null));
    }

    [Fact]
    public void DoubtfulRawOpinion_NeverVotesCompliant()
    {
        // Other-board doubtful is not a compliant vote and not a refusal.
        var (status, _) = ShariahVerdictService.ComputeOverall(
            ShariahStatus.Pending, false, new[] { "doubtful", "مشكوك" },
            isShariahIndexMember: false, activityCompliant: true,
            storedOverall: ShariahStatus.Pending, hasStoredRow: true);
        Assert.Equal(ShariahStatus.Pending, status);
    }
}
