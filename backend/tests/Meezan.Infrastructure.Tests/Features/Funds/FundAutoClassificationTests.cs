using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services;

namespace Meezan.Infrastructure.Tests.Features.Funds;

public class FundAutoClassificationTests
{
    private static Fund MakeFund(string nameAr = "صندوق اختبار", string nameEn = "Test Fund",
        string descriptionAr = "", string descriptionEn = "", string strategy = "",
        FundType type = FundType.Equity, bool isIslamic = false)
        => new()
        {
            SndukId = 1,
            PublicId = "P1",
            Slug = "test",
            NameAr = nameAr,
            NameEn = nameEn,
            Code = "TST",
            Type = type,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            InvestmentStrategy = strategy,
            IsIslamic = isIslamic,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SndukCreatedAt = DateTime.UtcNow,
            SndukUpdatedAt = DateTime.UtcNow
        };

    [Fact]
    public void IslamicRule_FollowsSndukFlag()
    {
        Assert.True(FundAutoClassification.Evaluate(MakeFund(isIslamic: true), """{"ruleType":"islamic"}""", out var reason));
        Assert.Contains("isIslamic", reason);
        Assert.False(FundAutoClassification.Evaluate(MakeFund(isIslamic: false), """{"ruleType":"islamic"}""", out _));
    }

    [Fact]
    public void IndexKeyword_MatchesEgx30Variants()
    {
        var fund = MakeFund(nameAr: "صندوق المؤشر EGX30", nameEn: "EGX 30 Index Fund");
        Assert.True(FundAutoClassification.Evaluate(fund, """{"keywords":["egx30","egx 30"],"field":"name"}""", out var reason));
        Assert.Contains("EGX", reason, StringComparison.OrdinalIgnoreCase);

        var other = MakeFund(nameAr: "صندوق أسهم متنوع");
        Assert.False(FundAutoClassification.Evaluate(other, """{"keywords":["egx30","egx 30"],"field":"name"}""", out _));
    }

    [Fact]
    public void ActiveRule_ExcludesIndexType()
    {
        Assert.True(FundAutoClassification.Evaluate(MakeFund(type: FundType.Equity), """{"ruleType":"active"}""", out _));
        Assert.False(FundAutoClassification.Evaluate(MakeFund(type: FundType.Index), """{"ruleType":"active"}""", out var reason));
        Assert.Contains("سلبي", reason);
    }

    [Fact]
    public void EmptyOrInvalidRule_NeverMatches()
    {
        var fund = MakeFund();
        Assert.False(FundAutoClassification.Evaluate(fund, null, out _));
        Assert.False(FundAutoClassification.Evaluate(fund, "", out _));
        Assert.False(FundAutoClassification.Evaluate(fund, "not-json", out _));
        Assert.False(FundAutoClassification.Evaluate(fund, """{"keywords":[],"field":"name"}""", out _));
    }

    [Fact]
    public void Defaults_ContainExpectedSlugs()
    {
        var slugs = FundAutoClassification.Defaults.Select(d => d.Slug).ToArray();
        Assert.Contains("sharia-compliant", slugs);
        Assert.Contains("index-egx30", slugs);
        Assert.Contains("index-egx33", slugs);
        Assert.Contains("index-egx70", slugs);
        Assert.Contains("index-egx100", slugs);
        Assert.Contains("actively-managed", slugs);
    }
}
