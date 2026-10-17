using Meezan.Application.Features.Scraping.Services;
using Meezan.Domain.Enums;

namespace Meezan.Infrastructure.Tests.Features.Stocks;

/// <summary>
/// Covers FairValueCalculator.ComputePremium, the single source of truth for
/// "distance from fair value":
///   premiumPct = (price − fairValue) / fairValue × 100
/// Positive = overvalued (Expensive), negative = undervalued (Cheap),
/// within ±2% = Fair, missing/invalid inputs = Unavailable with nulls.
/// </summary>
public class FairValuePremiumTests
{
    [Fact]
    public void PriceAboveFair_IsExpensive_WithPositivePremium()
    {
        // BIOC example: (348.22 − 41.8609) / 41.8609 × 100 ≈ +731.85%.
        var (diffAbs, diffPct, comparison) =
            FairValueCalculator.ComputePremium(41.8609m, 348.22m);

        Assert.Equal(PriceComparison.Expensive, comparison);
        Assert.NotNull(diffAbs);
        Assert.NotNull(diffPct);
        Assert.Equal(348.22m - 41.8609m, diffAbs!.Value);
        Assert.Equal(731.85m, diffPct!.Value, precision: 2);
        Assert.True(diffPct.Value > 0);
    }

    [Fact]
    public void PriceBelowFair_IsCheap_WithNegativePremium()
    {
        // (75 − 100) / 100 × 100 = −25%.
        var (diffAbs, diffPct, comparison) =
            FairValueCalculator.ComputePremium(100m, 75m);

        Assert.Equal(PriceComparison.Cheap, comparison);
        Assert.Equal(-25m, diffAbs!.Value);
        Assert.Equal(-25m, diffPct!.Value);
        Assert.True(diffPct.Value < 0);
    }

    [Theory]
    [InlineData(100.0, 101.0)] // +1% premium → Fair
    [InlineData(100.0, 99.0)]  // −1% premium → Fair
    [InlineData(100.0, 102.0)] // exactly +2% → Fair (tolerance is exclusive)
    [InlineData(100.0, 98.0)]  // exactly −2% → Fair
    public void WithinTolerance_IsFair(double fairValue, double price)
    {
        var (diffAbs, diffPct, comparison) =
            FairValueCalculator.ComputePremium((decimal)fairValue, (decimal)price);

        Assert.Equal(PriceComparison.Fair, comparison);
        Assert.NotNull(diffAbs);
        Assert.NotNull(diffPct);
    }

    [Theory]
    [InlineData(100.0, 102.01)] // just above +2% → Expensive
    [InlineData(100.0, 97.99)]  // just below −2% → Cheap
    public void OutsideTolerance_ClassifiesCorrectly(double fairValue, double price)
    {
        var (_, _, comparison) =
            FairValueCalculator.ComputePremium((decimal)fairValue, (decimal)price);

        Assert.Equal(price > fairValue ? PriceComparison.Expensive : PriceComparison.Cheap, comparison);
    }

    [Theory]
    [InlineData(0)]            // price = 0 → guard
    [InlineData(-5)]           // negative price → guard
    public void InvalidPrice_ReturnsUnavailable(double price)
    {
        var (diffAbs, diffPct, comparison) =
            FairValueCalculator.ComputePremium(100m, (decimal)price);

        Assert.Equal(PriceComparison.Unavailable, comparison);
        Assert.Null(diffAbs);
        Assert.Null(diffPct);
    }

    [Fact]
    public void NullPrice_ReturnsUnavailable()
    {
        var (diffAbs, diffPct, comparison) =
            FairValueCalculator.ComputePremium(100m, null);

        Assert.Equal(PriceComparison.Unavailable, comparison);
        Assert.Null(diffAbs);
        Assert.Null(diffPct);
    }

    [Theory]
    [InlineData(0)]            // fair = 0 → guard (division by zero)
    [InlineData(-10)]          // negative fair → guard
    public void InvalidFairValue_ReturnsUnavailable(double fairValue)
    {
        var (diffAbs, diffPct, comparison) =
            FairValueCalculator.ComputePremium((decimal)fairValue, 50m);

        Assert.Equal(PriceComparison.Unavailable, comparison);
        Assert.Null(diffAbs);
        Assert.Null(diffPct);
    }

    [Fact]
    public void NullFairValue_ReturnsUnavailable()
    {
        var (diffAbs, diffPct, comparison) =
            FairValueCalculator.ComputePremium(null, 50m);

        Assert.Equal(PriceComparison.Unavailable, comparison);
        Assert.Null(diffAbs);
        Assert.Null(diffPct);
    }

    [Fact]
    public void DiffAbs_Follows_SameSign_As_Premium()
    {
        // Overvalued: both positive; undervalued: both negative.
        var over = FairValueCalculator.ComputePremium(40m, 100m);
        Assert.True(over.DiffAbs > 0 && over.DiffPct > 0);

        var under = FairValueCalculator.ComputePremium(100m, 40m);
        Assert.True(under.DiffAbs < 0 && under.DiffPct < 0);
    }
}
