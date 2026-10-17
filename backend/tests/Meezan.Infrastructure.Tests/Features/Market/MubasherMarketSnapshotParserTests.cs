using Meezan.Infrastructure.Services;

namespace Meezan.Infrastructure.Tests.Features.Market;

/// <summary>
/// Covers the Mubasher summary-block number parsing: Western and Arabic-Indic
/// digits, commas, explicit signs, Unicode minus, parenthesized negatives —
/// and rejection of broken values (never invent numbers).
/// </summary>
public class MubasherMarketSnapshotParserTests
{
    [Theory]
    [InlineData("53,298.14", 53298.14)]
    [InlineData("5,994.71", 5994.71)]
    [InlineData("0.06", 0.06)]
    public void ParseDecimal_WesternDigits(string text, double expected)
    {
        Assert.Equal((decimal)expected, MubasherMarketSnapshotClient.ParseDecimal(text));
    }

    [Fact]
    public void ParseDecimal_ArabicIndicDigits()
    {
        Assert.Equal(53298.14m, MubasherMarketSnapshotClient.ParseDecimal("٥٣,٢٩٨.١٤"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("—")]
    [InlineData("N/A")]
    public void ParseDecimal_BrokenValues_ReturnNull(string? text)
    {
        Assert.Null(MubasherMarketSnapshotClient.ParseDecimal(text));
    }

    [Theory]
    [InlineData("-0.48%", -0.48)]
    [InlineData("+1.25%", 1.25)]
    [InlineData("1.25%", 1.25)]
    [InlineData("(0.50)", -0.50)]
    [InlineData("−0.48%", -0.48)] // U+2212 minus
    [InlineData("٨.٥٪", 8.5)]     // Arabic-Indic digits + Arabic percent sign
    public void ParseSignedPercent_Variants(string text, double expected)
    {
        Assert.Equal((decimal)expected, MubasherMarketSnapshotClient.ParseSignedPercent(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("--")]
    [InlineData("abc%")]
    public void ParseSignedPercent_BrokenValues_ReturnNull(string? text)
    {
        Assert.Null(MubasherMarketSnapshotClient.ParseSignedPercent(text));
    }

    [Fact]
    public void ParseSignedDecimal_MissingValue_ReturnNull()
    {
        Assert.Null(MubasherMarketSnapshotClient.ParseSignedDecimal(null));
        Assert.Null(MubasherMarketSnapshotClient.ParseSignedDecimal("  "));
        Assert.Equal(-255.27m, MubasherMarketSnapshotClient.ParseSignedDecimal("-255.27"));
    }
}
