namespace Meezan.Application.Common;

/// <summary>
/// Maps OUR stable keys to Mubasher market-page slugs
/// (https://www.mubasher.info/markets/EGX/indices/{slug}).
///
/// Index keys are our Index.Code values; sector keys are sector IDs.
/// Slugs were verified against the live site (server-rendered pages).
/// Sector slugs use Mubasher's 4-letter sector-index codes.
/// </summary>
public static class MubasherMarketMap
{
    /// <summary>Our index code → Mubasher slug. No entry = no published page.</summary>
    public static readonly IReadOnlyDictionary<string, string> IndexSlugs =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["EGX30"] = "EGX30",
            ["EGX30TR"] = "EGX30 TR",
            ["EGX35-LV"] = "EGX35-LV",
            ["EGX70"] = "EGX70 EWI",
            ["EGX100"] = "EGX100 EWI",
            ["TAMAYUZ"] = "TAMAYUZ",
            ["EGX 33"] = "SHARIAH",
            ["SHARIAH"] = "SHARIAH",
            ["EGX33"] = "SHARIAH"
        };

    /// <summary>Sector ID → Mubasher sector-index slug (verified live).</summary>
    public static readonly IReadOnlyDictionary<int, string> SectorSlugs =
        new Dictionary<int, string>
        {
            [1] = "BANK",   // Banks
            [2] = "BASC",   // Basic Resources
            [3] = "BULM",   // Building Materials
            [4] = "COCE",   // Contracting & Construction Engineering
            [5] = "EDUS",   // Education Services
            [6] = "ENGY",   // Energy & Support Services
            [7] = "FOBT",   // Food, Beverages and Tobacco
            [8] = "HLTH",   // Health Care & Pharmaceuticals
            [9] = "IMCS",   // IT, Media & Communication Services
            [10] = "IGSA",  // Industrial Goods, Services and Automobiles
            [11] = "NBFC",  // Non-bank financial services
            [12] = "REAL",  // Real Estate
            [13] = "SHTS",  // Shipping & Transportation Services
            [14] = "TEDU",  // Textile & Durables
            [15] = "TRDB",  // Trade & Distributors
            [16] = "TRVL"   // Travel & Leisure
        };
}
