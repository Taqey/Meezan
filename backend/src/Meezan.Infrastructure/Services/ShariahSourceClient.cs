using System.Text.Json;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services;

public class ShariahSourceClient : IShariahSourceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShariahSourceClient> _logger;
    private const string MergedDataUrl = "https://stocks.templatesnippet.com/data/stocks_merged.json";

    public ShariahSourceClient(HttpClient httpClient, ILogger<ShariahSourceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ExternalStockMergedDto>> FetchMergedStocksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching shariah merged stocks from {Url}", MergedDataUrl);
        var response = await _httpClient.GetAsync(MergedDataUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var result = new List<ExternalStockMergedDto>();
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("Expected JSON array from shariah external source");
            return result;
        }

        foreach (var element in document.RootElement.EnumerateArray())
        {
            try
            {
                var dto = ParseStock(element);
                if (!string.IsNullOrWhiteSpace(dto.Symbol))
                {
                    result.Add(dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing stock shariah item from merged JSON");
            }
        }

        _logger.LogInformation("Successfully parsed {Count} stocks from external shariah source", result.Count);
        return result;
    }

    private static ExternalStockMergedDto ParseStock(JsonElement elem)
    {
        var dto = new ExternalStockMergedDto
        {
            Id = GetString(elem, "id"),
            Symbol = GetString(elem, "symbol") ?? string.Empty,
            NameEn = GetString(elem, "name_en"),
            NameAr = GetString(elem, "name_ar"),
            Market = GetString(elem, "market"),
            Currency = GetString(elem, "currency"),
            LastUpdated = GetDateTime(elem, "last_updated"),
            CreatedAt = GetDateTime(elem, "created_at"),
            UpdatedAt = GetDateTime(elem, "updated_at"),

            Zakat = GetDecimal(elem, "zakat"),
            SpHaramEarningPercentage = GetDecimal(elem, "sp_haram_earning_percentage"),
            AaoifiHaramEarningPerShare = GetDecimal(elem, "aaoifi_haram_earning_per_share"),
            HaramEarningsPercentage = GetDecimal(elem, "haram_earnings_percentage"),
            LoansPercentage = GetDecimal(elem, "loans_percentage"),
            FairValueValuation = GetDecimal(elem, "fair_value_valuation"),
            BookValue = GetDecimal(elem, "book_value"),
            Profit = GetDecimal(elem, "profit"),
            Dividend = GetDecimal(elem, "dividend"),
            DividendType = GetString(elem, "dividend_type"),
            CoreActivityCompliant = GetBool(elem, "core_activity_compliant"),
            CashLiquidityCompliant = GetBool(elem, "cash_liquidity_compliant"),
            HaramInvestmentsCompliant = GetBool(elem, "haram_investments_compliant"),
            Category = GetString(elem, "category"),
            CategoryAr = GetString(elem, "category_ar"),

            HalalBourse = ParseSourceOpinion(elem, "halal_bourse"),
            Musaffa = ParseSourceOpinion(elem, "musaffa", "grade", "sector", "industry", "ranking"),
            Kashif = ParseSourceOpinion(elem, "kashif", "purity", "haram_percentage", "sector", "statement_date"),
            HalalInvest = ParseSourceOpinion(elem, "halal_invest"),
            FaisalBank = ParseSourceOpinion(elem, "faisal_bank"),
            Ostoul = ParseSourceOpinion(elem, "osoul", "haram_percentage", "avg_market_cap", "total_assets", "deposits_percentage", "loans_percentage", "liquid_assets_percentage", "sector"),
            Thndr = ParseSourceOpinion(elem, "thndr")
        };

        return dto;
    }

    private static ExternalSourceOpinionDto? ParseSourceOpinion(JsonElement parent, string propName, params string[] extraKeys)
    {
        if (!parent.TryGetProperty(propName, out var elem) || elem.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var dto = new ExternalSourceOpinionDto
        {
            Status = GetString(elem, "status"),
            Percentage = GetDecimal(elem, "percentage"),
            Note = GetString(elem, "note"),
            PdfUrl = GetString(elem, "pdf_url"),
            LastUpdated = GetDateTime(elem, "last_updated")
        };

        // Collect extra fields
        var extraDict = new Dictionary<string, object?>();
        foreach (var key in extraKeys)
        {
            if (elem.TryGetProperty(key, out var extraVal))
            {
                extraDict[key] = extraVal.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => extraVal.GetString(),
                    JsonValueKind.Number => extraVal.TryGetDecimal(out var d) ? d : extraVal.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => extraVal.GetRawText()
                };
            }
        }

        if (extraDict.Count > 0)
        {
            dto.ExtraDataJson = JsonSerializer.Serialize(extraDict);
        }

        return dto;
    }

    private static string? GetString(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
        {
            return val.GetString();
        }
        return null;
    }

    private static decimal? GetDecimal(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val))
        {
            if (val.ValueKind == JsonValueKind.Number && val.TryGetDecimal(out var d))
            {
                return d;
            }
            if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var parsed))
            {
                return parsed;
            }
        }
        return null;
    }

    private static bool? GetBool(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val))
        {
            if (val.ValueKind == JsonValueKind.True) return true;
            if (val.ValueKind == JsonValueKind.False) return false;
            if (val.ValueKind == JsonValueKind.String && bool.TryParse(val.GetString(), out var b)) return b;
        }
        return null;
    }

    private static DateTime? GetDateTime(JsonElement elem, string prop)
    {
        if (elem.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(val.GetString(), out var dt))
            {
                return dt;
            }
        }
        return null;
    }
}
