using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models;

public sealed class SndukFundListResponse
{
    public SndukFundDto[] Funds { get; set; } = [];
    public SndukFundResult[]? Result { get; set; }
}

public sealed class SndukFundResult
{
    public SndukFundData? Data { get; set; }
}

public sealed class SndukFundData
{
    public SndukFundJson? Json { get; set; }
}

public sealed class SndukFundJson
{
    public SndukFundDto[] Funds { get; set; } = [];
}

public sealed class SndukFundDto
{
    public long Id { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PublicId { get; set; } = string.Empty;
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Slug { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CustomSlug { get; set; }
    public string Name { get; set; } = string.Empty;
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? NameEn { get; set; }
    public string Code { get; set; } = string.Empty;
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Type { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TypeNameAr { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TypeNameEn { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? RiskLevel { get; set; }
    public bool? IsIslamic { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsActive { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CurrentPrice { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? DailyChange { get; set; }
    public SndukReturnsDto? Returns { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TotalScore { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Aum { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? ManagementFee { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? MinimumInvestment { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Currency { get; set; }
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long? AssetManagerId { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? AssetManagerName { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? AssetManagerNameEn { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? AssetManagerCode { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? LogoUrl { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Description { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? DescriptionEn { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? InvestmentStrategy { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? InceptionDate { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? ProspectusUrl { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CreatedAt { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? UpdatedAt { get; set; }
}

public sealed class SndukReturnsDto
{
    [JsonPropertyName("1M")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? M1 { get; set; }
    [JsonPropertyName("3M")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? M3 { get; set; }
    [JsonPropertyName("6M")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? M6 { get; set; }
    [JsonPropertyName("1Y")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Y1 { get; set; }
    [JsonPropertyName("3Y")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Y3 { get; set; }
}

public sealed class SndukPerformanceResponse
{
    public SndukPricePointDto[] Data { get; set; } = [];
    public SndukPerformanceSummaryDto? Summary { get; set; }
    public SndukPerformanceResult[]? Result { get; set; }
}

public sealed class SndukPerformanceResult
{
    public SndukPerformanceData? Data { get; set; }
}

public sealed class SndukPerformanceData
{
    public SndukPerformanceJson Json { get; set; } = new();
}

public sealed class SndukPerformanceJson
{
    public SndukPricePointDto[] Data { get; set; } = [];
    public SndukPerformanceSummaryDto? Summary { get; set; }
}

public sealed class SndukPricePointDto
{
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Date { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Price { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Change { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Performance { get; set; }
}

public sealed class SndukPerformanceSummaryDto
{
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TotalReturn { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Volatility { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? MaxDrawdown { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SharpeRatio { get; set; }
}

public sealed class SndukAnalysisResponse
{
    public SndukScoresDto? Scores { get; set; }
    public SndukRawMetricsDto? RawMetrics { get; set; }
    public JsonElement? Weights { get; set; }
    public SndukExplanationsDto? Explanations { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CalculatedAt { get; set; }
    public SndukAnalysisResult[]? Result { get; set; }
}

public sealed class SndukAnalysisResult
{
    public SndukAnalysisData? Data { get; set; }
}

public sealed class SndukAnalysisData
{
    public SndukAnalysisJson Json { get; set; } = new();
}

public sealed class SndukAnalysisJson
{
    public SndukScoresDto? Scores { get; set; }
    public SndukRawMetricsDto? RawMetrics { get; set; }
    public JsonElement? Weights { get; set; }
    public SndukExplanationsDto? Explanations { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CalculatedAt { get; set; }
}

public sealed class SndukScoresDto
{
    public decimal? Performance { get; set; }
    public decimal? Risk { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Stability { get; set; }
    public decimal? Diversification { get; set; }
    public decimal? Total { get; set; }
}

public sealed class SndukRawMetricsDto
{
    public decimal? Cagr { get; set; }
    public decimal? Alpha { get; set; }
    public decimal? Volatility { get; set; }
    public decimal? Consistency { get; set; }
    public decimal? MaxDrawdown { get; set; }
    public decimal? SharpeRatio { get; set; }
}

public sealed class SndukExplanationsDto
{
    public SndukExplanationDto? Performance { get; set; }
    public SndukExplanationDto? Risk { get; set; }
    public SndukExplanationDto? Cost { get; set; }
    public SndukExplanationDto? Stability { get; set; }
    public SndukExplanationDto? Diversification { get; set; }
}

public sealed class SndukExplanationDto
{
    public string[]? Ar { get; set; }
    public string[]? En { get; set; }
}

public sealed class SndukCompetingResponse
{
    public SndukFundDto[] Funds { get; set; } = [];
    public SndukCompetingResult[]? Result { get; set; }
}

public sealed class SndukCompetingResult
{
    public SndukCompetingData? Data { get; set; }
}

public sealed class SndukCompetingData
{
    public SndukCompetingJson Json { get; set; } = new();
}

public sealed class SndukCompetingJson
{
    public SndukFundDto[] Funds { get; set; } = [];
}

public sealed class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to string.")
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}

public sealed class FlexibleLongConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => long.TryParse(reader.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null,
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to long.")
        };

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteNumberValue(value.Value);
    }
}
