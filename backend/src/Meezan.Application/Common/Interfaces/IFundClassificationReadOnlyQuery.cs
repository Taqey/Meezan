using Meezan.Domain.Models;

namespace Meezan.Application.Common.Interfaces;

public interface IFundClassificationReadOnlyQuery
{
    Task<ClassificationContext> LoadContextAsync(CancellationToken ct = default);
    Task<EffectiveClassification> ResolveEffectiveAsync(Meezan.Domain.Entities.Fund fund, ClassificationContext ctx);
    Task<Dictionary<int, EffectiveClassification>> ResolveEffectiveManyAsync(IEnumerable<Meezan.Domain.Entities.Fund> funds, ClassificationContext ctx);
    Task<List<Meezan.Domain.Entities.FundClassificationNode>> GetPublicTreeAsync(CancellationToken ct = default);
    Task<List<string>> GetPathSlugsAsync(int nodeId, CancellationToken ct = default);
    Task<List<Meezan.Domain.Entities.FundClassificationNode>> GetPathAsync(int nodeId, CancellationToken ct = default);
    Task<HashSet<int>> GetDescendantIdsAsync(int nodeId, CancellationToken ct = default);
}
