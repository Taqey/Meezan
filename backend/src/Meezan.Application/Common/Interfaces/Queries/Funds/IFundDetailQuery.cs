using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Domain.Models;

namespace Meezan.Application.Common.Interfaces.Queries.Funds;

public interface IFundDetailQuery
{
    Task<FundDetailBundle?> LoadAsync(int fundId, CancellationToken ct = default);
    Task<ClassificationContext> LoadContextAsync(CancellationToken ct = default);
    Task<Dictionary<int, EffectiveClassification>> ResolveEffectiveManyAsync(IEnumerable<Fund> funds, ClassificationContext ctx);
}
