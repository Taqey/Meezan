using Meezan.Application.Common.Models.Caching;

namespace Meezan.Application.Common.Interfaces.Caching;

/// <summary>Cached static data (index + constituents, no prices) keyed by index code.</summary>
public interface IIndexStaticCache
{
    /// <summary>Returns the static entry, or null when the index does not exist.</summary>
    Task<IndexStaticEntry?> GetAsync(string indexCode, CancellationToken ct = default);
}
