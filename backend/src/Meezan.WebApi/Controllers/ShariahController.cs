using System.Text.Json;
using MediatR;
using Meezan.Application.Features.Shariah.Commands.RefreshShariahData;
using Meezan.Application.Features.Shariah.Commands.SeedShariahMap;
using Meezan.Application.Features.Shariah.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/shariah")]
public class ShariahController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public ShariahController(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    /// <summary>
    /// Seeds ShariahCompliance from shariah_map.json (bundled resource or uploaded payload).
    /// Creates any missing Stock rows by SymbolCode.
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(SeedShariahMapResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Seed([FromBody] Dictionary<string, ShariahSeedItemDto>? payload, CancellationToken cancellationToken)
    {
        Dictionary<string, ShariahSeedItemDto>? data = payload;

        // If no payload passed, read from embedded resource or local file
        if (data == null || data.Count == 0)
        {
            data = await LoadBundledShariahMapAsync(cancellationToken);
        }

        if (data == null || data.Count == 0)
        {
            return BadRequest(new { message = "No seed data found in request body or bundled shariah_map.json resource." });
        }

        var result = await _mediator.Send(new SeedShariahMapCommand(data), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Manually triggers a refresh of external shariah data (from stocks_merged.json).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshShariahDataResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshShariahDataCommand(TriggeredBy: "Manual"), cancellationToken);
        return Ok(result);
    }

    private async Task<Dictionary<string, ShariahSeedItemDto>?> LoadBundledShariahMapAsync(CancellationToken cancellationToken)
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
            if (System.IO.File.Exists(path))
            {
                // Open with explicit UTF-8 (BOM-tolerant) so Arabic text is preserved.
                await using var fs = System.IO.File.OpenRead(path);
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
                    // Read with explicit UTF-8 so Arabic notes survive deserialization.
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
