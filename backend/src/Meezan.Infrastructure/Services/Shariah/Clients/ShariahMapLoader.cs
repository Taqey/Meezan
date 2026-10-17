using System.Text.Json;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Features.Shariah.DTOs;
using Microsoft.Extensions.Hosting;

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
