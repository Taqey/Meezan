using Meezan.Domain.Entities;
using Meezan.Domain.Models;

namespace Meezan.Application.Common.Interfaces;

public interface IFundClassificationService
{
    Task<ClassificationMigrationReport> EnsureSeedAndMigrateAsync(string performedBy, CancellationToken ct = default);
    Task SyncFundClassificationAsync(int fundId, CancellationToken ct = default);
    Task<ClassificationContext> LoadContextAsync(CancellationToken ct = default);
    Task<EffectiveClassification> ResolveEffectiveAsync(Fund fund, ClassificationContext ctx);
    Task<Dictionary<int, EffectiveClassification>> ResolveEffectiveManyAsync(IEnumerable<Fund> funds, ClassificationContext ctx);
    Task<List<FundClassificationNode>> GetPathAsync(int nodeId, CancellationToken ct = default);
    Task<HashSet<int>> GetDescendantIdsAsync(int nodeId, CancellationToken ct = default);
    Task<List<string>> GetPathSlugsAsync(int nodeId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> ValidateMoveAsync(int nodeId, int? newParentId, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> ValidateSlugAsync(string slug, int? ignoreId, CancellationToken ct = default);
    Task<ReviewQueue> GetReviewQueueAsync(CancellationToken ct = default);
    Task<ReclassifyResult> ReclassifyUnclassifiedAsync(string performedBy, CancellationToken ct = default);
    Task<List<FundClassificationNode>> GetPublicTreeAsync(CancellationToken ct = default);
}
