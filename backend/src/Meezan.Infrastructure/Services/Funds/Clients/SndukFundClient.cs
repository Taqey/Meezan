using Meezan.Application.Common.Interfaces.Integrations;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Meezan.Infrastructure.Services.Funds.Clients;

public sealed class SndukFundClient : ISndukFundClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SndukFundClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public SndukFundClient(HttpClient http, ILogger<SndukFundClient> logger)
    {
        _http = http;
        _logger = logger;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _http.DefaultRequestHeaders.Referrer = new Uri("https://snduk.com/eg/funds?lang=ar");
    }

    public async Task<SndukFundListResponse?> GetFundListAsync(CancellationToken cancellationToken = default)
    {
        var input = new
        {
            json = new
            {
                type = (string?)null,
                country = "EG",
                search = (string?)null,
                minInvestment = 0,
                maxInvestment = 1000000,
                minPerformance = 0,
                maxPerformance = 100,
                isIslamic = (bool?)null,
                categoryId = (string?)null,
                sortBy = "performance",
                sortDirection = "desc",
                isActive = true,
                limit = 1000,
                offset = 0
            },
            meta = new
            {
                values = new
                {
                    type = new[] { "undefined" },
                    search = new[] { "undefined" },
                    isIslamic = new[] { "undefined" },
                    categoryId = new[] { "undefined" }
                },
                v = 1
            }
        };

        // tRPC batch protocol: the input object must be wrapped as {"0": {...}}
        var batchInput = new Dictionary<string, object> { ["0"] = input };
        var inputJson = JsonSerializer.Serialize(batchInput, JsonOptions);
        var encodedInput = Uri.EscapeDataString(inputJson);
        var url = $"https://snduk.com/api/trpc/funds.list?batch=1&input={encodedInput}";

        try
        {
            var response = await _http.GetAsync(url, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var trpcError = TryExtractTrpcError(json);
                throw new HttpRequestException(
                    $"funds.list failed with HTTP {(int)response.StatusCode}{(trpcError != null ? $" ({trpcError})" : "")}. Body: {TruncateForLog(json)}");
            }
            return DeserializeTResponse<SndukFundListResponse>(json, "funds.list");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch fund list from SNDUK");
            throw;
        }
    }

    public async Task<SndukPerformanceResponse?> GetFundPerformanceAsync(long fundId, string timeframe = "1Y", CancellationToken cancellationToken = default)
    {
        var input = new { json = new { fundId = fundId.ToString(), timeframe } };
        var batchInput = new Dictionary<string, object> { ["0"] = input };
        var inputJson = JsonSerializer.Serialize(batchInput, JsonOptions);
        var encodedInput = Uri.EscapeDataString(inputJson);
        var url = $"https://snduk.com/api/trpc/funds.getPerformance?batch=1&input={encodedInput}";

        try
        {
            var response = await _http.GetAsync(url, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch performance for fund {FundId} (timeframe: {Timeframe}): {StatusCode}. Body: {Body}",
                    fundId, timeframe, (int)response.StatusCode, TruncateForLog(json));
                return null;
            }
            return DeserializeTResponse<SndukPerformanceResponse>(json, "funds.getPerformance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch performance for fund {FundId}", fundId);
            return null;
        }
    }

    public async Task<SndukAnalysisResponse?> GetFundAnalysisAsync(long fundId, CancellationToken cancellationToken = default)
    {
        var input = new { json = new { fundId } };
        var batchInput = new Dictionary<string, object> { ["0"] = input };
        var inputJson = JsonSerializer.Serialize(batchInput, JsonOptions);
        var encodedInput = Uri.EscapeDataString(inputJson);
        var url = $"https://snduk.com/api/trpc/funds.getAnalysis?batch=1&input={encodedInput}";

        try
        {
            var response = await _http.GetAsync(url, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch analysis for fund {FundId}: {StatusCode}. Body: {Body}",
                    fundId, (int)response.StatusCode, TruncateForLog(json));
                return null;
            }
            return DeserializeTResponse<SndukAnalysisResponse>(json, "funds.getAnalysis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch analysis for fund {FundId}", fundId);
            return null;
        }
    }

    public async Task<SndukCompetingResponse?> GetFundCompetingAsync(long fundId, CancellationToken cancellationToken = default)
    {
        var input = new { json = new { fundId } };
        var batchInput = new Dictionary<string, object> { ["0"] = input };
        var inputJson = JsonSerializer.Serialize(batchInput, JsonOptions);
        var encodedInput = Uri.EscapeDataString(inputJson);
        var url = $"https://snduk.com/api/trpc/funds.getCompeting?batch=1&input={encodedInput}";

        try
        {
            var response = await _http.GetAsync(url, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch competing funds for fund {FundId}: {StatusCode}. Body: {Body}",
                    fundId, (int)response.StatusCode, TruncateForLog(json));
                return null;
            }
            return DeserializeTResponse<SndukCompetingResponse>(json, "funds.getCompeting");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch competing funds for fund {FundId}", fundId);
            return null;
        }
    }

    private T? DeserializeTResponse<T>(string json, string endpointName) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning("{Endpoint} returned empty response body", endpointName);
            return null;
        }

        // Log first 200 chars for debugging (never the full body)
        var preview = TruncateForLog(json);
        _logger.LogDebug("{Endpoint} raw response: {Preview}", endpointName, preview);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Invalid JSON from {endpointName}. Body preview: {preview}", ex);
        }
        using (doc)
        {
            // tRPC batch responses are arrays
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException($"Expected JSON array from {endpointName}, got {doc.RootElement.ValueKind}. Body preview: {preview}");
            }

            var array = doc.RootElement.EnumerateArray().ToArray();
            if (array.Length == 0)
            {
                throw new JsonException($"Empty response array from {endpointName}. Body preview: {preview}");
            }

            var first = array[0];

            // Check for tRPC error format: { "error": { "message": "...", "code": ... } }
            if (first.TryGetProperty("error", out var errorElement))
            {
                var message = errorElement.GetPropertyOrDefault("message", "Unknown tRPC error");
                var code = errorElement.GetPropertyOrDefault("code", "UNKNOWN");
                throw new HttpRequestException($"{endpointName} returned tRPC error [{code}]: {message}");
            }

            // Expect { "result": { "data": { "json": <payload> } } }
            if (!first.TryGetProperty("result", out var resultElement))
            {
                throw new JsonException($"Missing 'result' in response from {endpointName}. Body preview: {preview}");
            }

            if (!resultElement.TryGetProperty("data", out var dataElement))
            {
                throw new JsonException($"Missing 'result.data' in response from {endpointName}. Body preview: {preview}");
            }

            if (!dataElement.TryGetProperty("json", out var jsonElement))
            {
                throw new JsonException($"Missing 'result.data.json' in response from {endpointName}. Body preview: {preview}");
            }

            var payloadJson = jsonElement.ValueKind == JsonValueKind.Array && typeof(T) == typeof(SndukCompetingResponse)
                ? "{\"funds\":" + jsonElement.GetRawText() + "}"
                : jsonElement.GetRawText();
            try
            {
                return JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize payload for {Endpoint}. Payload: {Payload}", endpointName, payloadJson.Length > 500 ? payloadJson.Substring(0, 500) + "..." : payloadJson);
                throw;
            }
        }
    }

    private static string TruncateForLog(string body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;
        return body.Length > 200 ? body.Substring(0, 200) + "..." : body;
    }

    private static string? TryExtractTrpcError(string body)
    {
        try
        {
            using var errorDoc = JsonDocument.Parse(body);
            if (errorDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var first = errorDoc.RootElement.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object
                    && first.TryGetProperty("error", out var errorElement))
                {
                    var message = errorElement.GetPropertyOrDefault("message", "Unknown tRPC error");
                    var codeElement = errorElement.TryGetProperty("code", out var c) ? c : (JsonElement?)null;
                    var code = codeElement.HasValue
                        ? (codeElement.Value.ValueKind == JsonValueKind.Number
                            ? codeElement.Value.GetRawText()
                            : codeElement.Value.GetString() ?? "UNKNOWN")
                        : "UNKNOWN";
                    return $"[{code}]: {message}";
                }
            }
        }
        catch (JsonException)
        {
            // Not parseable — caller already logs the body preview.
        }
        return null;
    }
}

// Helper extension method
internal static class JsonElementExtensions
{
    public static string GetPropertyOrDefault(this JsonElement element, string propertyName, string defaultValue)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
        {
            return prop.ValueKind == JsonValueKind.Number
                ? prop.GetRawText()
                : prop.GetString() ?? defaultValue;
        }
        return defaultValue;
    }
}
