using Meezan.Domain.Entities;

namespace Meezan.Infrastructure.Tests.Features.Funds;

/// <summary>
/// Pure unit tests for the shared automatic-classification rules: index signals
/// (EGX30/33/70/100/35), the six sector keyword rules, unclassified fallback,
/// conflict handling, non-equity behavior, the auto on/off switch, SNDUK mapping
/// and the commodities→silver alias. No database involved.
/// </summary>
public class FundClassificationRulesTests
{
    private static int _id;

    private static FundClassificationNode Node(string slug, string? parentSlug, int level,
        string ruleJson = "", bool visible = true, bool requiresChild = false)
    {
        return new FundClassificationNode
        {
            Id = ++_id,
            Slug = slug,
            NameAr = slug,
            NameEn = slug,
            Level = level,
            ParentId = null, // wired below via slug map when building paths
            Icon = string.Empty,
            Color = string.Empty,
            DisplayOrder = 1,
            IsVisible = visible,
            IsDeleted = false,
            DeletedReason = string.Empty,
            RequiresChildSelection = requiresChild,
            ChildrenLabelAr = "التصنيف الفرعي",
            ChildrenLabelEn = "Sub classification",
            AutoRuleJson = ruleJson,
            AutoClassificationEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            UpdatedBy = "test"
        };
    }

    /// <summary>Minimal tree mirroring the seed: L0 tops + equity children + index/sector leaves.</summary>
    private static ClassificationContext BuildContext(bool autoEnabled = true, string? activeRule = null)
    {
        _id = 0;
        var nodes = new List<FundClassificationNode>
        {
            Node("equity", null, 0, requiresChild: true),
            Node("fixed_income", null, 0),
            Node("fixed_income_usd", null, 0),
            Node("money_market", null, 0),
            Node("balanced", null, 0),
            Node("gold", null, 0),
            Node("silver", null, 0),
            Node("capital_preservation", null, 0),
            Node("sector", "equity", 1, requiresChild: true),
            Node("index", "equity", 1, requiresChild: true),
            Node("active", "equity", 1, activeRule ?? ""),
            Node("equity-unclassified", "equity", 1, visible: false),
            Node("index-egx30", "index", 2, """{"keywords":["egx30","egx 30"],"field":"name"}"""),
            Node("index-egx33", "index", 2, """{"keywords":["egx33","egx 33"],"field":"name"}"""),
            Node("index-egx70", "index", 2, """{"keywords":["egx70","egx 70"],"field":"name"}"""),
            Node("index-egx100", "index", 2, """{"keywords":["egx100","egx 100"],"field":"name"}"""),
            Node("tracks-egx35", "index", 2, """{"keywords":["egx35","egx 35"],"field":"name"}"""),
            Node("sector-financial", "sector", 2, """{"keywords":["المالي","البنوك","financial","banks","bank"],"field":"name"}"""),
            Node("sector-real-estate", "sector", 2, """{"keywords":["العقاري","عقارات","عقاري","real estate"],"field":"name"}"""),
            Node("sector-consumer", "sector", 2, """{"keywords":["الاستهلاكي","استهلاكي","consumer"],"field":"name"}"""),
            Node("sector-industrial", "sector", 2, """{"keywords":["الصناعي","صناعي","industrial"],"field":"name"}"""),
            Node("sector-technology", "sector", 2, """{"keywords":["التكنولوجيا","تكنولوجي","تقنية","technology","tech"],"field":"name"}"""),
            Node("sector-ipo", "sector", 2, """{"keywords":["الاكتتابات","اكتتاب","ipo"],"field":"name"}"""),
        };
        var bySlug = nodes.ToDictionary(n => n.Slug, StringComparer.OrdinalIgnoreCase);
        var parentOf = new Dictionary<string, string?>
        {
            ["sector"] = "equity",
            ["index"] = "equity",
            ["active"] = "equity",
            ["equity-unclassified"] = "equity",
            ["index-egx30"] = "index",
            ["index-egx33"] = "index",
            ["index-egx70"] = "index",
            ["index-egx100"] = "index",
            ["tracks-egx35"] = "index",
            ["sector-financial"] = "sector",
            ["sector-real-estate"] = "sector",
            ["sector-consumer"] = "sector",
            ["sector-industrial"] = "sector",
            ["sector-technology"] = "sector",
            ["sector-ipo"] = "sector",
        };
        foreach (var (slug, parent) in parentOf)
        {
            bySlug[slug].ParentId = parent == null ? null : bySlug[parent].Id;
        }
        return new ClassificationContext
        {
            Nodes = nodes,
            ById = nodes.ToDictionary(n => n.Id),
            BySlug = new Dictionary<string, FundClassificationNode>(bySlug, StringComparer.OrdinalIgnoreCase),
            OverridesByFund = new Dictionary<int, List<FundClassificationOverride>>(),
            AutoEnabled = autoEnabled
        };
    }

    private static Fund EquityFund(string nameAr, string nameEn = "", string strategy = "", string desc = "")
    {
        return new Fund
        {
            Id = Random.Shared.Next(100000, 999999),
            SndukId = Random.Shared.NextInt64(),
            Code = "T" + Random.Shared.Next(1000, 9999),
            Slug = Guid.NewGuid().ToString("N"),
            PublicId = Guid.NewGuid().ToString("N"),
            NameAr = nameAr,
            NameEn = nameEn,
            Type = FundType.Equity,
            IsActive = true,
            DescriptionAr = desc,
            InvestmentStrategy = strategy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SndukCreatedAt = DateTime.UtcNow,
            SndukUpdatedAt = DateTime.UtcNow
        };
    }

    // ---------- index signals ----------

    [Theory]
    [InlineData("صندوق مؤشر EGX30", "index-egx30")]
    [InlineData("EGX 33 Sharia Fund", "index-egx33")]
    [InlineData("صندوق مؤشر EGX70", "index-egx70")]
    [InlineData("EGX100 Tracker", "index-egx100")]
    [InlineData("صندوق EGX35 للأسهم", "tracks-egx35")]
    public void IndexSignal_MapsToMatchingChild(string name, string expectedSlug)
    {
        var ctx = BuildContext();
        var eff = FundClassificationService.ResolveEffective(EquityFund(name), ctx);
        Assert.Equal(expectedSlug, eff.Slug);
        Assert.Equal("Auto", eff.Source);
        Assert.False(eff.NeedsReview);
        Assert.NotEmpty(eff.Reason);
        Assert.Equal(3, eff.PathSlugs.Count); // equity > index > child
        Assert.Equal("equity", eff.PathSlugs[0]);
        Assert.Equal("index", eff.PathSlugs[1]);
    }

    // ---------- sector signals ----------

    [Theory]
    [InlineData("صندوق الاستثمار المالي", "sector-financial")]
    [InlineData("Mid Bank Fund", "sector-financial")]
    [InlineData("صندوق العقارات المصري", "sector-real-estate")]
    [InlineData("Real Estate Growth Fund", "sector-real-estate")]
    [InlineData("صندوق السلع الاستهلاكية", "sector-consumer")]
    [InlineData("Consumer Staples Fund", "sector-consumer")]
    [InlineData("الصندوق الصناعي", "sector-industrial")]
    [InlineData("Industrial Development Fund", "sector-industrial")]
    [InlineData("صندوق التكنولوجيا", "sector-technology")]
    [InlineData("Tech Innovation Fund", "sector-technology")]
    [InlineData("صندوق الاكتتابات الأولية", "sector-ipo")]
    [InlineData("IPO Opportunities Fund", "sector-ipo")]
    public void SectorSignal_MapsToMatchingChild(string name, string expectedSlug)
    {
        var ctx = BuildContext();
        var eff = FundClassificationService.ResolveEffective(EquityFund(name), ctx);
        Assert.Equal(expectedSlug, eff.Slug);
        Assert.Equal("Auto", eff.Source);
        Assert.Equal(3, eff.PathSlugs.Count);
        Assert.Equal("equity", eff.PathSlugs[0]);
        Assert.Equal("sector", eff.PathSlugs[1]);
    }

    // ---------- unclassified fallback ----------

    [Fact]
    public void NoSignal_GoesToUnclassified_NotActive()
    {
        var ctx = BuildContext();
        var eff = FundClassificationService.ResolveEffective(EquityFund("صندوق النمو المتوازن"), ctx);
        Assert.Equal("equity-unclassified", eff.Slug);
        Assert.Equal("Auto", eff.Source);
        Assert.NotEqual("active", eff.Slug);
    }

    [Fact]
    public void ActiveNode_WithoutRule_NeverAutoAssigned()
    {
        var ctx = BuildContext();
        var names = new[] { "صندوق النمو", "Growth Fund", "صندوق الأسهم المصرية", "Blue Chip Fund" };
        foreach (var n in names)
        {
            var eff = FundClassificationService.ResolveEffective(EquityFund(n), ctx);
            Assert.NotEqual("active", eff.Slug);
        }
    }

    [Fact]
    public void ActiveNode_WithExplicitRule_CanAutoAssign()
    {
        var ctx = BuildContext(activeRule: """{"keywords":["نشط مختار"],"field":"name"}""");
        var eff = FundClassificationService.ResolveEffective(EquityFund("صندوق نشط مختار بعناية"), ctx);
        Assert.Equal("active", eff.Slug);
        Assert.Equal("Auto", eff.Source);
    }

    // ---------- conflicts ----------

    [Fact]
    public void ConflictingIndexSignals_GoUnclassified_WithReason()
    {
        var ctx = BuildContext();
        var eff = FundClassificationService.ResolveEffective(EquityFund("صندوق EGX30 و EGX70 معاً"), ctx);
        Assert.Equal("equity-unclassified", eff.Slug);
        Assert.True(eff.NeedsReview);
        Assert.NotEmpty(eff.Reason);
        Assert.NotEmpty(eff.ReviewReasons);
    }

    [Fact]
    public void IndexPlusSectorSignals_GoUnclassified_WithReason()
    {
        var ctx = BuildContext();
        var eff = FundClassificationService.ResolveEffective(EquityFund("EGX30 Financial Fund"), ctx);
        Assert.Equal("equity-unclassified", eff.Slug);
        Assert.True(eff.NeedsReview);
    }

    [Fact]
    public void NameVsDescriptionDisagreement_FlaggedForReview()
    {
        var ctx = BuildContext();
        var fund = EquityFund("صندوق مؤشر EGX30", desc: "يستثمر في شركات مؤشر EGX70");
        var eff = FundClassificationService.ResolveEffective(fund, ctx);
        Assert.Equal("equity-unclassified", eff.Slug);
        Assert.True(eff.NeedsReview);
    }

    // ---------- non-equity ----------

    [Fact]
    public void NonEquityFund_GetsNoEquitySubtype()
    {
        var ctx = BuildContext();
        var fund = EquityFund("صندوق EGX30 النقدي");
        fund.Type = FundType.MoneyMarket;
        var eff = FundClassificationService.ResolveEffective(fund, ctx);
        Assert.Equal("money_market", eff.Slug);
        Assert.Equal("SNDUK", eff.Source);
    }

    [Fact]
    public void NonEquityNode_WithExplicitRule_ClassifiesChild()
    {
        var ctx = BuildContext();
        var parent = ctx.BySlug["fixed_income"];
        var child = new FundClassificationNode
        {
            Id = 999,
            Slug = "fixed-testchild",
            NameAr = "فرعي اختبار",
            NameEn = "Test Child",
            Level = 1,
            ParentId = parent.Id,
            Icon = string.Empty,
            Color = string.Empty,
            DisplayOrder = 1,
            IsVisible = true,
            IsDeleted = false,
            DeletedReason = string.Empty,
            RequiresChildSelection = false,
            ChildrenLabelAr = "التصنيف الفرعي",
            ChildrenLabelEn = "Sub classification",
            AutoRuleJson = """{"keywords":["TESTKEY-XYZ"],"field":"name"}""",
            AutoClassificationEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            UpdatedBy = "test"
        };
        ctx.Nodes.Add(child);
        ctx.ById[child.Id] = child;
        ctx.BySlug[child.Slug] = child;
        var fund = EquityFund("صندوق TESTKEY-XYZ للدخل");
        fund.Type = FundType.FixedIncome;
        var eff = FundClassificationService.ResolveEffective(fund, ctx);
        Assert.Equal("fixed-testchild", eff.Slug);
        Assert.Equal("Auto", eff.Source);
        Assert.Equal(2, eff.PathSlugs.Count);
        Assert.Equal("fixed_income", eff.PathSlugs[0]);
    }

    // ---------- auto switch ----------

    [Fact]
    public void AutoDisabled_SkipsKeywordRules_UsesSndukBaseline()
    {
        var ctx = BuildContext(autoEnabled: false);
        var eff = FundClassificationService.ResolveEffective(EquityFund("صندوق مؤشر EGX30"), ctx);
        Assert.Equal("equity", eff.Slug);
        Assert.Equal("SNDUK", eff.Source);
    }

    // ---------- SNDUK mapping + silver alias ----------

    [Fact]
    public void MapSnduk_Commodities_GoesToSilver()
    {
        Assert.Equal("silver", FundClassificationService.MapSndukTopLevel("commodities", FundType.Commodities));
        Assert.Equal("silver", FundClassificationService.MapSndukTopLevel(null, FundType.Commodities));
    }

    [Fact]
    public void MapSnduk_Index_GoesToEquityBranch()
    {
        Assert.Equal("equity", FundClassificationService.MapSndukTopLevel("index", FundType.Index));
    }

    [Fact]
    public void FundTypeCatalog_SilverAlias_RoundTrips()
    {
        Assert.Equal("silver", FundTypeCatalog.SlugFor(FundType.Commodities));
        Assert.Equal(FundType.Commodities, FundTypeCatalog.EnumFor("silver"));
        Assert.Equal(FundType.Commodities, FundTypeCatalog.EnumFor("commodities"));
    }

    [Fact]
    public void UnknownIncomingType_ParksUnderUnclassified_WithReviewFlag()
    {
        var ctx = BuildContext();
        var fund = EquityFund("صندوق غامض");
        fund.Type = FundType.Unknown;
        var eff = FundClassificationService.ResolveEffective(fund, ctx);
        Assert.Equal(ctx.BySlug["equity-unclassified"].Id, eff.NodeId);
        Assert.Equal("equity-unclassified", eff.Slug);
        Assert.True(eff.NeedsReview);
    }

    // ---------- manual precedence (static) ----------

    [Fact]
    public void ManualInclude_WinsOverAuto()
    {
        var ctx = BuildContext();
        var fund = EquityFund("صندوق مؤشر EGX30");
        var active = ctx.BySlug["active"];
        ctx.OverridesByFund[fund.Id] = new List<FundClassificationOverride>
        {
            new() { FundId = fund.Id, NodeId = active.Id, Action = ClassificationOverrideAction.Include, CreatedBy = "admin", CreatedAt = DateTime.UtcNow }
        };
        var eff = FundClassificationService.ResolveEffective(fund, ctx);
        Assert.Equal("active", eff.Slug);
        Assert.Equal("Manual", eff.Source);
    }

    // ---------- text helpers ----------

    [Fact]
    public void NormalizeAr_UnifiesAlefForms()
    {
        Assert.Equal(FundClassificationService.NormalizeAr("إيجي إكس"), FundClassificationService.NormalizeAr("ايجي اكس"));
    }
}
