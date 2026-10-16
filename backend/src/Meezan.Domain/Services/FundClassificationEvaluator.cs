using System.Text.Json;
using Meezan.Domain.Entities;
using Meezan.Domain.Models;

namespace Meezan.Domain.Services;

public static class FundClassificationEvaluator
{
    public const string UnclassifiedSlug = "equity-unclassified";
    public const string SilverSlug = "silver";
    public const string CommoditiesSlug = "commodities";

    public static string MapSndukTopLevel(string? sndukType, FundType enumType)
    {
        var t = (sndukType ?? string.Empty).Trim().ToLowerInvariant();
        return t switch
        {
            "commodities" => SilverSlug,
            "index" => "equity",
            "equity" => "equity",
            "balanced" => "balanced",
            "fixed_income" => "fixed_income",
            "fixed_income_usd" => "fixed_income_usd",
            "money_market" => "money_market",
            "gold" => "gold",
            "silver" => SilverSlug,
            "capital_preservation" => "capital_preservation",
            _ => FundTypeCatalog.SlugFor(enumType) == "commodities" ? SilverSlug : FundTypeCatalog.SlugFor(enumType)
        };
    }

    public static string NormalizeAr(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.ToLowerInvariant();
        s = s.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا')
             .Replace('ة', 'ه').Replace('ى', 'ي').Replace('ؤ', 'و').Replace('ئ', 'ي');
        return s;
    }

    public static bool KeywordHit(string normalizedText, string keyword)
    {
        var k = NormalizeAr(keyword.Trim());
        if (string.IsNullOrEmpty(k)) return false;
        bool isLatin = k.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == ' ' || c == '-' || c == '_');
        if (isLatin && k.Replace(" ", "").Length <= 4 && !k.Contains(' '))
        {
            int idx = 0;
            while ((idx = normalizedText.IndexOf(k, idx, StringComparison.Ordinal)) >= 0)
            {
                bool leftOk = idx == 0 || !char.IsLetterOrDigit(normalizedText[idx - 1]);
                int end = idx + k.Length;
                bool rightOk = end >= normalizedText.Length || !char.IsLetterOrDigit(normalizedText[end]);
                if (leftOk && rightOk) return true;
                idx++;
            }
            return false;
        }
        return normalizedText.Contains(k);
    }

    public sealed class RuleShape
    {
        public string[] Keywords { get; set; } = [];
        public string Field { get; set; } = "name";
        public string? RuleType { get; set; }
    }

    public static RuleShape? ParseRule(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<RuleShape>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException) { return null; }
    }

    public static string FundSearchText(Fund fund)
    {
        return NormalizeAr($"{fund.NameAr} {fund.NameEn} {fund.DescriptionAr} {fund.DescriptionEn} {fund.InvestmentStrategy} {fund.TypeNameAr} {fund.TypeNameEn}");
    }

    public static EffectiveClassification ResolveEffective(Fund fund, ClassificationContext ctx)
    {
        // 1. Manual assignment wins: latest manual Include.
        if (ctx.OverridesByFund.TryGetValue(fund.Id, out var list))
        {
            var manual = list.Where(o => o.Action == ClassificationOverrideAction.Include && !string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).FirstOrDefault();
            if (manual != null && ctx.ById.TryGetValue(manual.NodeId, out var mNode) && !mNode.IsDeleted)
            {
                var path = FundClassificationTreeEngine.BuildPath(mNode, ctx.ById);
                return new EffectiveClassification
                {
                    NodeId = mNode.Id,
                    Slug = mNode.Slug,
                    PathSlugs = path.Select(n => n.Slug).ToList(),
                    PathNamesAr = path.Select(n => n.NameAr).ToList(),
                    PathNamesEn = path.Select(n => n.NameEn).ToList(),
                    Source = "Manual",
                    Reason = "تعيين يدوي",
                    NeedsReview = mNode.RequiresChildSelection && !HasSelectedChild(fund.Id, mNode.Id, ctx),
                    ReviewReasons = mNode.RequiresChildSelection && !HasSelectedChild(fund.Id, mNode.Id, ctx)
                        ? new List<string> { "العقدة تتطلب اختيار تصنيف فرعي" } : new List<string>()
                };
            }

            // 2. Stored automatic result (written by sync / reclassify, recalculated regularly).
            var stored = list.Where(o => o.Action == ClassificationOverrideAction.Include && string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).FirstOrDefault();
            if (stored != null && ctx.ById.TryGetValue(stored.NodeId, out var sNode) && !sNode.IsDeleted)
            {
                var path = FundClassificationTreeEngine.BuildPath(sNode, ctx.ById);
                return new EffectiveClassification
                {
                    NodeId = sNode.Id,
                    Slug = sNode.Slug,
                    PathSlugs = path.Select(n => n.Slug).ToList(),
                    PathNamesAr = path.Select(n => n.NameAr).ToList(),
                    PathNamesEn = path.Select(n => n.NameEn).ToList(),
                    Source = "Auto",
                    Reason = LiveReason(fund, sNode, ctx),
                    NeedsReview = false,
                    ReviewReasons = new List<string>()
                };
            }
        }

        // 2. Automatic keyword rules (when enabled).
        if (ctx.AutoEnabled)
        {
            var auto = EvaluateAuto(fund, ctx);
            if (auto.Node != null)
                return auto.Result!;
            if (auto.Conflict != null)
            {
                var u = RequireNode(ctx, UnclassifiedSlug);
                if (u != null)
                {
                    var path = FundClassificationTreeEngine.BuildPath(u, ctx.ById);
                    return new EffectiveClassification
                    {
                        NodeId = u.Id,
                        Slug = u.Slug,
                        PathSlugs = path.Select(n => n.Slug).ToList(),
                        PathNamesAr = path.Select(n => n.NameAr).ToList(),
                        PathNamesEn = path.Select(n => n.NameEn).ToList(),
                        Source = "Auto",
                        Reason = auto.Conflict,
                        NeedsReview = true,
                        ReviewReasons = new List<string> { auto.Conflict }
                    };
                }
            }
            else if (auto.NoSignal && IsEquityLike(fund))
            {
                var u2 = RequireNode(ctx, UnclassifiedSlug);
                if (u2 != null)
                {
                    var path = FundClassificationTreeEngine.BuildPath(u2, ctx.ById);
                    return new EffectiveClassification
                    {
                        NodeId = u2.Id,
                        Slug = u2.Slug,
                        PathSlugs = path.Select(n => n.Slug).ToList(),
                        PathNamesAr = path.Select(n => n.NameAr).ToList(),
                        PathNamesEn = path.Select(n => n.NameEn).ToList(),
                        Source = "Auto",
                        Reason = "لا توجد إشارة تصنيف موثوقة",
                        NeedsReview = false,
                        ReviewReasons = new List<string>()
                    };
                }
            }
        }

        // 3. SNDUK baseline mapping.
        var topSlug = MapSndukTopLevel(null, fund.Type);
        if (string.Equals(topSlug, "unknown", StringComparison.OrdinalIgnoreCase) || !ctx.BySlug.TryGetValue(topSlug, out var top) || top.IsDeleted)
        {
            var u = RequireNode(ctx, UnclassifiedSlug);
            if (u != null)
            {
                var upath = FundClassificationTreeEngine.BuildPath(u, ctx.ById);
                return new EffectiveClassification
                {
                    NodeId = u.Id,
                    Slug = u.Slug,
                    PathSlugs = upath.Select(n => n.Slug).ToList(),
                    PathNamesAr = upath.Select(n => n.NameAr).ToList(),
                    PathNamesEn = upath.Select(n => n.NameEn).ToList(),
                    Source = "SNDUK",
                    Reason = "نوع SNDUK غير معروف ولا يوجد تعيين يدوي",
                    NeedsReview = true,
                    ReviewReasons = new List<string> { "نوع SNDUK غير معروف" }
                };
            }
            return new EffectiveClassification
            {
                NodeId = null,
                Slug = string.Empty,
                Source = "None",
                Reason = "نوع SNDUK غير معروف ولا يوجد تعيين يدوي",
                NeedsReview = true,
                ReviewReasons = new List<string> { "نوع SNDUK غير معروف" }
            };
        }

        var topPath = FundClassificationTreeEngine.BuildPath(top, ctx.ById);
        return new EffectiveClassification
        {
            NodeId = top.Id,
            Slug = top.Slug,
            PathSlugs = topPath.Select(n => n.Slug).ToList(),
            PathNamesAr = topPath.Select(n => n.NameAr).ToList(),
            PathNamesEn = topPath.Select(n => n.NameEn).ToList(),
            Source = "SNDUK",
            Reason = "تعيين SNDUK الأساسي",
            NeedsReview = top.RequiresChildSelection,
            ReviewReasons = top.RequiresChildSelection ? new List<string> { "العقدة تتطلب اختيار تصنيف فرعي" } : new List<string>()
        };
    }

    private sealed class AutoOutcome
    {
        public FundClassificationNode? Node;
        public EffectiveClassification? Result;
        public string? Conflict;
        public bool NoSignal;
    }

    private static string LiveReason(Fund fund, FundClassificationNode node, ClassificationContext ctx)
    {
        var text = FundSearchText(fund);
        var rule = ParseRule(node.AutoRuleJson);
        if (rule?.Keywords is { Length: > 0 })
        {
            foreach (var kw in rule.Keywords)
            {
                if (KeywordHit(text, kw)) return $"طابق '{kw}' في الاسم/الوصف";
            }
        }
        if (string.Equals(node.Slug, UnclassifiedSlug, StringComparison.OrdinalIgnoreCase))
            return "لا توجد إشارة تصنيف موثوقة";
        return "تصنيف تلقائي";
    }

    private static bool IsEquityLike(Fund fund)
        => fund.Type == FundType.Equity || fund.Type == FundType.Index;

    private static FundClassificationNode? RequireNode(ClassificationContext ctx, string slug)
        => ctx.BySlug.TryGetValue(slug, out var n) && !n.IsDeleted ? n : null;

    private static bool HasSelectedChild(int fundId, int nodeId, ClassificationContext ctx)
    {
        if (!ctx.OverridesByFund.TryGetValue(fundId, out var list)) return false;
        var desc = FundClassificationTreeEngine.DescendantIds(ctx.Nodes, nodeId);
        return list.Any(o => o.Action == ClassificationOverrideAction.Include && desc.Contains(o.NodeId));
    }

    private static AutoOutcome EvaluateAuto(Fund fund, ClassificationContext ctx)
    {
        var outcome = new AutoOutcome();
        var text = FundSearchText(fund);
        bool equityLike = IsEquityLike(fund);

        var indexMatches = new List<(FundClassificationNode Node, string Keyword)>();
        var sectorMatches = new List<(FundClassificationNode Node, string Keyword)>();

        if (ctx.BySlug.TryGetValue("index", out var indexNode) && !indexNode.IsDeleted)
        {
            foreach (var child in ctx.Nodes.Where(n => n.ParentId == indexNode.Id && !n.IsDeleted && n.AutoClassificationEnabled))
            {
                var rule = ParseRule(child.AutoRuleJson);
                if (rule?.Keywords == null || rule.Keywords.Length == 0) continue;
                foreach (var kw in rule.Keywords)
                {
                    if (KeywordHit(text, kw)) { indexMatches.Add((child, kw)); break; }
                }
            }
            if (fund.Type == FundType.Index && indexMatches.Count == 0)
            {
                indexMatches.Add((null!, "snduk:index"));
            }
        }

        if (ctx.BySlug.TryGetValue("sector", out var sectorNode) && !sectorNode.IsDeleted)
        {
            foreach (var child in ctx.Nodes.Where(n => n.ParentId == sectorNode.Id && !n.IsDeleted && n.AutoClassificationEnabled))
            {
                var rule = ParseRule(child.AutoRuleJson);
                if (rule?.Keywords == null || rule.Keywords.Length == 0) continue;
                foreach (var kw in rule.Keywords)
                {
                    if (KeywordHit(text, kw)) { sectorMatches.Add((child, kw)); break; }
                }
            }
        }

        FundClassificationNode? activeByRule = null;
        if (ctx.BySlug.TryGetValue("active", out var activeNode) && !activeNode.IsDeleted && activeNode.AutoClassificationEnabled)
        {
            var rule = ParseRule(activeNode.AutoRuleJson);
            if (rule?.Keywords is { Length: > 0 })
            {
                foreach (var kw in rule.Keywords)
                {
                    if (KeywordHit(text, kw)) { activeByRule = activeNode; break; }
                }
            }
        }

        if (!equityLike)
        {
            var topSlug = MapSndukTopLevel(null, fund.Type);
            if (ctx.BySlug.TryGetValue(topSlug, out var top) && !top.IsDeleted)
            {
                var hits = new List<FundClassificationNode>();
                foreach (var child in ctx.Nodes.Where(n => n.ParentId == top.Id && !n.IsDeleted && n.AutoClassificationEnabled))
                {
                    var rule = ParseRule(child.AutoRuleJson);
                    if (rule?.Keywords == null || rule.Keywords.Length == 0) continue;
                    foreach (var kw in rule.Keywords)
                    {
                        if (KeywordHit(text, kw)) { hits.Add(child); break; }
                    }
                }
                if (hits.Count == 1)
                {
                    var path = FundClassificationTreeEngine.BuildPath(hits[0], ctx.ById);
                    outcome.Node = hits[0];
                    outcome.Result = ToEffective(hits[0], path, "Auto", $"طابق '{hits[0].Slug}' في الاسم/الوصف");
                    return outcome;
                }
                if (hits.Count > 1)
                {
                    outcome.Conflict = $"إشارات متضاربة: {string.Join("، ", hits.Select(h => h.NameAr))}";
                    return outcome;
                }
            }
            outcome.NoSignal = false;
            return outcome;
        }

        var realIndex = indexMatches.Where(m => m.Node != null).ToList();
        var genericIndexOnly = indexMatches.Count == 1 && indexMatches[0].Node == null;

        if (realIndex.Select(m => m.Node.Id).Distinct().Count() > 1)
        {
            outcome.Conflict = $"إشارات مؤشر متضاربة: {string.Join("، ", realIndex.Select(m => m.Node.NameAr).Distinct())}";
            return outcome;
        }
        if (realIndex.Count >= 1 && sectorMatches.Count >= 1)
        {
            outcome.Conflict = $"إشارتا مؤشر وقطاع معاً: {realIndex[0].Node.NameAr} + {sectorMatches[0].Node.NameAr}";
            return outcome;
        }
        if (sectorMatches.Select(m => m.Node.Id).Distinct().Count() > 1)
        {
            outcome.Conflict = $"إشارات قطاعية متضاربة: {string.Join("، ", sectorMatches.Select(m => m.Node.NameAr).Distinct())}";
            return outcome;
        }
        if (realIndex.Count == 1 && sectorMatches.Count == 0)
        {
            var path = FundClassificationTreeEngine.BuildPath(realIndex[0].Node, ctx.ById);
            outcome.Node = realIndex[0].Node;
            outcome.Result = ToEffective(realIndex[0].Node, path, "Auto", $"طابق '{realIndex[0].Keyword}' في الاسم/الوصف");
            return outcome;
        }
        if (sectorMatches.Count == 1 && realIndex.Count == 0 && !genericIndexOnly)
        {
            var path = FundClassificationTreeEngine.BuildPath(sectorMatches[0].Node, ctx.ById);
            outcome.Node = sectorMatches[0].Node;
            outcome.Result = ToEffective(sectorMatches[0].Node, path, "Auto", $"طابق '{sectorMatches[0].Keyword}' في الاسم/الوصف");
            return outcome;
        }
        if (activeByRule != null && realIndex.Count == 0 && sectorMatches.Count == 0)
        {
            var path = FundClassificationTreeEngine.BuildPath(activeByRule, ctx.ById);
            outcome.Node = activeByRule;
            outcome.Result = ToEffective(activeByRule, path, "Auto", "طابق قاعدة النشط المخصصة");
            return outcome;
        }
        if (genericIndexOnly)
        {
            outcome.Conflict = "إشارة مؤشر SNDUK بدون مؤشر محدد في الاسم/الوصف";
            return outcome;
        }
        outcome.NoSignal = true;
        return outcome;
    }

    private static EffectiveClassification ToEffective(FundClassificationNode node, List<FundClassificationNode> path, string source, string reason)
        => new()
        {
            NodeId = node.Id,
            Slug = node.Slug,
            PathSlugs = path.Select(n => n.Slug).ToList(),
            PathNamesAr = path.Select(n => n.NameAr).ToList(),
            PathNamesEn = path.Select(n => n.NameEn).ToList(),
            Source = source,
            Reason = reason,
            NeedsReview = false,
            ReviewReasons = new List<string>()
        };
}
