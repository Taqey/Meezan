using Meezan.Domain.Entities;
using System.Text.Json;

namespace Meezan.Domain.Services;

/// <summary>
/// Pure auto-classification logic for fund categories (no DB access).
/// Rule JSON shapes:
///   {"ruleType":"islamic"}                 -> SNDUK isIslamic flag
///   {"ruleType":"active"}                  -> not an index-type fund
///   {"keywords":[...],"field":"name"}      -> keyword match in name/description/strategy/type
/// Manual include/exclude overrides always win over this evaluation (enforced by the caller).
/// </summary>
public static class FundAutoClassification
{
    public sealed record DefaultCategory(string Slug, string NameAr, string NameEn, string AutoRuleJson, int DisplayOrder);

    public static readonly IReadOnlyList<DefaultCategory> Defaults = new List<DefaultCategory>
    {
        new("sharia-compliant", "متوافق مع الشريعة", "Sharia-compliant", """{"ruleType":"islamic"}""", 1),
        new("index-egx30", "يتبع مؤشر EGX30", "Tracks EGX30", """{"keywords":["egx30","egx 30"],"field":"name"}""", 2),
        new("index-egx33", "يتبع مؤشر EGX33", "Tracks EGX33", """{"keywords":["egx33","egx 33"],"field":"name"}""", 3),
        new("index-egx70", "يتبع مؤشر EGX70", "Tracks EGX70", """{"keywords":["egx70","egx 70"],"field":"name"}""", 4),
        new("index-egx100", "يتبع مؤشر EGX100", "Tracks EGX100", """{"keywords":["egx100","egx 100"],"field":"name"}""", 5),
        new("actively-managed", "مدار بنشاط", "Actively managed", """{"ruleType":"active"}""", 6),
    };

    public sealed class AutoCategoryRule
    {
        public string[] Keywords { get; set; } = [];
        public string Field { get; set; } = "name"; // name, description, strategy, type, all
        public string? RuleType { get; set; }        // "islamic", "active", null for keyword match
    }

    public static bool EvaluateRule(FundCategory category, Fund fund)
    {
        if (!category.IsAutoDerived || string.IsNullOrWhiteSpace(category.AutoRuleJson))
            return false;

        AutoCategoryRule? rule;
        try
        {
            rule = JsonSerializer.Deserialize<AutoCategoryRule>(category.AutoRuleJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return false;
        }

        if (rule == null) return false;

        if (string.Equals(rule.RuleType, "islamic", StringComparison.OrdinalIgnoreCase))
            return fund.IsIslamic;

        if (string.Equals(rule.RuleType, "active", StringComparison.OrdinalIgnoreCase))
            return fund.Type != FundType.Index;

        if (rule.Keywords.Length == 0)
            return false;

        var textToSearch = (rule.Field?.ToLowerInvariant() switch
        {
            "description" => $"{fund.DescriptionAr} {fund.DescriptionEn}",
            "strategy" => fund.InvestmentStrategy ?? string.Empty,
            "type" => $"{fund.TypeNameAr} {fund.TypeNameEn}",
            "all" => $"{fund.NameAr} {fund.NameEn} {fund.DescriptionAr} {fund.DescriptionEn} {fund.InvestmentStrategy} {fund.TypeNameAr} {fund.TypeNameEn}",
            _ => $"{fund.NameAr} {fund.NameEn}" // default to name
        }).ToLowerInvariant();

        return rule.Keywords.Any(kw => textToSearch.Contains(kw.Trim().ToLowerInvariant()));
    }

    public static bool Evaluate(Fund fund, string? autoRuleJson, out string reason)
    {
        reason = string.Empty;
        AutoCategoryRule? rule = null;
        if (!string.IsNullOrWhiteSpace(autoRuleJson))
        {
            try
            {
                rule = JsonSerializer.Deserialize<AutoCategoryRule>(autoRuleJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException) { rule = null; }
        }

        if (rule?.RuleType != null)
        {
            switch (rule.RuleType.Trim().ToLowerInvariant())
            {
                case "islamic":
                    if (fund.IsIslamic)
                    {
                        reason = "SNDUK isIslamic = true (تصنيف المصدر)";
                        return true;
                    }
                    return false;
                case "active":
                    if (fund.Type != FundType.Index)
                    {
                        reason = $"النوع {fund.Type} ليس مؤشراً (يُدار بنشاط)";
                        return true;
                    }
                    reason = "صندوق مؤشر (يُدار بشكل سلبي)";
                    return false;
            }
        }

        if (rule == null || rule.Keywords == null || rule.Keywords.Length == 0)
            return false;

        var searchText = $"{fund.NameAr} {fund.NameEn} {fund.DescriptionAr} {fund.DescriptionEn} {fund.InvestmentStrategy} {fund.TypeNameAr} {fund.TypeNameEn}".ToLowerInvariant();
        foreach (var keyword in rule.Keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) && searchText.Contains(keyword.ToLowerInvariant()))
            {
                reason = $"طابق '{keyword}' في الاسم/الوصف";
                return true;
            }
        }
        return false;
    }
}

