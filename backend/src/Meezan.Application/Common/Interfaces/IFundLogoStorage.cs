namespace Meezan.Application.Common.Interfaces;

public interface IFundLogoStorage
{
    Task<string?> StoreAsync(string sourceUrl, string fundSlug, CancellationToken ct = default);
}
