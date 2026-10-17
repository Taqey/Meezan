namespace Meezan.Application.Common.Interfaces.Storage;

public interface IFundLogoStorage
{
    Task<string?> StoreAsync(string sourceUrl, string fundSlug, CancellationToken ct = default);
}
