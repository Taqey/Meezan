using Meezan.Domain.Entities;

namespace Meezan.Infrastructure.Services.Funds.Classification;

/// <summary>
/// Canonical list of fund types: one row per <see cref="FundType"/> enum value.
/// The slug is the stable identity mapped to the enum; labels/order/visibility
/// are seeded once and thereafter operator-editable.
/// </summary>
public static class FundTypeCatalog
{
    public sealed record TypeSeed(string Slug, string NameAr, string NameEn, int DisplayOrder);

    public static readonly IReadOnlyList<TypeSeed> Seeds = new List<TypeSeed>
    {
        new("equity",              "صندوق أسهم",              "Equity Fund",           1),
        new("fixed_income",        "صندوق دخل ثابت",          "Fixed Income Fund",     2),
        new("fixed_income_usd",    "صندوق دخل ثابت بالدولار", "USD Fixed Income Fund", 3),
        new("balanced",            "صندوق متوازن",            "Balanced Fund",         4),
        new("money_market",        "صندوق سوق نقدي",          "Money Market Fund",     5),
        new("index",               "صندوق مؤشر",              "Index Fund",            6),
        new("gold",                "صندوق ذهب",               "Gold Fund",             7),
        new("commodities",         "صندوق سلع",               "Commodities Fund",      8),
        new("capital_preservation","صندوق الحفاظ على رأس المال","Capital Preservation Fund", 9),
        new("unknown",             "غير محدد",                "Unclassified",          99)
    };

    /// <summary>
    /// Canonical slug for a stored enum value. The stored enum member
    /// <see cref="FundType.Commodities"/> is exposed as "silver" everywhere
    /// (tree, filters, labels); "commodities" remains accepted as an alias.
    /// </summary>
    public static string SlugFor(FundType type) => type switch
    {
        FundType.Balanced            => "balanced",
        FundType.CapitalPreservation => "capital_preservation",
        FundType.Commodities         => "silver",
        FundType.Equity              => "equity",
        FundType.FixedIncome         => "fixed_income",
        FundType.FixedIncomeUsd      => "fixed_income_usd",
        FundType.Gold                => "gold",
        FundType.Index               => "index",
        FundType.MoneyMarket         => "money_market",
        _                            => "unknown"
    };

    /// <summary>
    /// Accepts both "silver" and the legacy alias "commodities".
    /// </summary>
    public static FundType EnumFor(string slug) => slug switch
    {
        "balanced"             => FundType.Balanced,
        "capital_preservation" => FundType.CapitalPreservation,
        "commodities"          => FundType.Commodities,
        "silver"               => FundType.Commodities,
        "equity"               => FundType.Equity,
        "fixed_income"         => FundType.FixedIncome,
        "fixed_income_usd"     => FundType.FixedIncomeUsd,
        "gold"                 => FundType.Gold,
        "index"                => FundType.Index,
        "money_market"         => FundType.MoneyMarket,
        "unknown"              => FundType.Unknown,
        _                      => FundType.Unknown
    };
}
