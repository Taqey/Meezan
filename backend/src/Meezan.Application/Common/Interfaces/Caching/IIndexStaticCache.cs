using Meezan.Application.Common.Models.Caching;

namespace Meezan.Application.Common.Interfaces.Caching;

/// <summary>Cached static data (index + constituents, no prices) keyed by index code.</summary>
public interface IIndexStaticCache
{
    /// <summary>Returns the static entry, or null when the index does not exist.</summary>
    Task<IndexStaticEntry?> GetAsync(string indexCode, CancellationToken ct = default);

    /// <summary>Invalidates the cache (L1 and L2) for the given index code or all indices if null.</summary>
    Task InvalidateAsync(string? indexCode = null, CancellationToken ct = default);
}
