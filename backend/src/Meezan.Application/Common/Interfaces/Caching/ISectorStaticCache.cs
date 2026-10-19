using Meezan.Application.Common.Models.Caching;

namespace Meezan.Application.Common.Interfaces.Caching;

/// <summary>Cached static sector definitions (Id, NameAr, NameEn).</summary>
public interface ISectorStaticCache
{
    /// <summary>Returns the list of static sector entries (from L1/L2 or database).</summary>
    Task<List<SectorStaticEntry>> GetAsync(CancellationToken ct = default);
}
