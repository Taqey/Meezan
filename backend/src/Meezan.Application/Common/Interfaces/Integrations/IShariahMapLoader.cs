using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Common.Interfaces.Integrations;

public interface IShariahMapLoader
{
    Task<Dictionary<string, ShariahSeedItemDto>?> LoadBundledShariahMapAsync(CancellationToken cancellationToken = default);
}
