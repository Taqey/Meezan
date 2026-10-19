using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Models.Indices;

namespace Meezan.Application.Common.Interfaces.Queries.Indices;

/// <summary>
/// Read-side query for the public index constituents endpoint. Constant number of SQL
/// commands regardless of the number of constituents; implemented in Infrastructure.
/// </summary>
public interface IIndexConstituentReadQuery
{
    /// <summary>Returns null when the index does not exist.</summary>
    Task<IndexConstituentsPage?> QueryPageAsync(
        string indexCode, ConstituentListFilter filter, CancellationToken ct = default);
}
