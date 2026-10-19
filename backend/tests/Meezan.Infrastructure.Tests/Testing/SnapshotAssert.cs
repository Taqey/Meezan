using System.Text.Json;
using System.Text.Json.Nodes;

namespace Meezan.Infrastructure.Tests.Testing;

public class EndpointSnapshot
{
    public int StatusCode { get; set; }
    public JsonNode? BodyShape { get; set; }

    public static EndpointSnapshot FromResponse(int statusCode, string content)
    {
        JsonNode? shape = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                var parsed = JsonNode.Parse(content);
                shape = NormalizeShape(parsed);
            }
            catch
            {
                shape = JsonValue.Create("NON_JSON_CONTENT");
            }
        }

        return new EndpointSnapshot
        {
            StatusCode = statusCode,
            BodyShape = shape
        };
    }

    private static JsonNode? NormalizeShape(JsonNode? node)
    {
        if (node == null) return null;

        if (node is JsonObject obj)
        {
            var result = new JsonObject();
            foreach (var kvp in obj)
            {
                // We keep property names and casing, but normalize dynamic trace/request IDs
                if (kvp.Key.Equals("traceId", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Equals("requestId", StringComparison.OrdinalIgnoreCase))
                {
                    result[kvp.Key] = JsonValue.Create("string:trace-id-normalized");
                }
                else if (kvp.Key.Equals("duration", StringComparison.OrdinalIgnoreCase))
                {
                    result[kvp.Key] = JsonValue.Create("string:duration-normalized");
                }
                else if (kvp.Key.Equals("elapsedSeconds", StringComparison.OrdinalIgnoreCase))
                {
                    result[kvp.Key] = JsonValue.Create("number:seconds-normalized");
                }
                else
                {
                    result[kvp.Key] = NormalizeShape(kvp.Value);
                }
            }
            return result;
        }

        if (node is JsonArray arr)
        {
            var result = new JsonArray();
            if (arr.Count > 0)
            {
                // Normalize to the shape of the elements
                result.Add(NormalizeShape(arr[0]));
            }
            return result;
        }

        if (node is JsonValue val)
        {
            if (val.TryGetValue<string>(out var str))
            {
                if (DateTime.TryParse(str, out _) && (str.Contains('T') || str.Contains('-') && str.Length >= 10))
                {
                    return JsonValue.Create("string:date-time-normalized");
                }
                // Keep specific error/status titles or messages, otherwise normalize
                return JsonValue.Create($"string:{str}");
            }
            if (val.TryGetValue<long>(out _) || val.TryGetValue<int>(out _))
            {
                return JsonValue.Create("number:int");
            }
            if (val.TryGetValue<decimal>(out _) || val.TryGetValue<double>(out _))
            {
                return JsonValue.Create("number:decimal");
            }
            if (val.TryGetValue<bool>(out var b))
            {
                return JsonValue.Create($"bool:{b}");
            }
        }

        return JsonValue.Create("value");
    }
}

public static class SnapshotAssert
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static void MatchesStoredSnapshot(string snapshotName, int statusCode, string responseContent, string snapshotDir = "Snapshots")
    {
        var current = EndpointSnapshot.FromResponse(statusCode, responseContent);
        var currentJson = JsonSerializer.Serialize(current, Options);

        var dir = Path.Combine(AppContext.BaseDirectory, snapshotDir);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{snapshotName}.json");

        if (!File.Exists(path) || Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "true")
        {
            File.WriteAllText(path, currentJson);

            // Also update source tree snapshot file if found
            var solutionDir = ArchitectureTests.FindSolutionDirectory();
            if (solutionDir != null)
            {
                var srcSnapshotPath = Path.Combine(solutionDir, "tests", "Meezan.Infrastructure.Tests", snapshotDir, $"{snapshotName}.json");
                if (Directory.Exists(Path.GetDirectoryName(srcSnapshotPath)))
                {
                    File.WriteAllText(srcSnapshotPath, currentJson);
                }
            }
            return;
        }

        var expectedJson = File.ReadAllText(path);
        Xunit.Assert.Equal(NormalizeLineEndings(expectedJson), NormalizeLineEndings(currentJson));
    }

    private static string NormalizeLineEndings(string s) => s.Replace("\r\n", "\n").Trim();
}
