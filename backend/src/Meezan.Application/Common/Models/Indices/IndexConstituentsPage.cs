using Meezan.Application.Common.Interfaces.Repositories.Indices;

namespace Meezan.Application.Common.Models.Indices;

/// <summary>One page of an index's constituents plus the index header data.</summary>
public sealed record IndexConstituentsPage(
    string IndexCode,
    string IndexNameAr,
    string IndexNameEn,
    DateTime? LastUpdated,
    List<ConstituentProjection> Items,
    int TotalCount);
