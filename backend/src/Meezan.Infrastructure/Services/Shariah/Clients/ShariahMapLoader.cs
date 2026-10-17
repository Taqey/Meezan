using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Features.Shariah.DTOs;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace Meezan.Infrastructure.Services.Shariah.Clients;

public class ShariahMapLoader : IShariahMapLoader
{
    private readonly IHostEnvironment _env;

    public ShariahMapLoader(IHostEnvironment env)
    {
        _env = env;
    }

    public async Task<Dictionary<string, ShariahSeedItemDto>?> LoadBundledShariahMapAsync(CancellationToken cancellationToken = default)
    {
        // 1. Try file path next to executing directory or app root
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "shariah_map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "shariah_map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "shariah_map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "shariah_map.json"),
            Path.Combine(_env.ContentRootPath, "shariah_map.json"),
            Path.Combine(_env.ContentRootPath, "..", "shariah_map.json"),
            Path.Combine(_env.ContentRootPath, "..", "..", "shariah_map.json")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                // Open with explicit UTF-8 (BOM-tolerant) so Arabic text is preserved.
                await using var fs = File.OpenRead(path);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var json = await reader.ReadToEndAsync(cancellationToken);
                return JsonSerializer.Deserialize<Dictionary<string, ShariahSeedItemDto>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }

        // 2. Try embedded resource from Meezan.Infrastructure assembly
        var infraAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Meezan.Infrastructure");

        if (infraAssembly != null)
        {
            var resName = infraAssembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("shariah_map.json", StringComparison.OrdinalIgnoreCase));

            if (resName != null)
            {
                await using var stream = infraAssembly.GetManifestResourceStream(resName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    var json = await reader.ReadToEndAsync(cancellationToken);
                    return JsonSerializer.Deserialize<Dictionary<string, ShariahSeedItemDto>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
        }

        return null;
    }
}
