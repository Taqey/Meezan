using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundAnalysisRepository
{
    Task<FundAnalysis?> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default);
    Task AddAsync(FundAnalysis analysis, CancellationToken cancellationToken = default);
    void Update(FundAnalysis analysis);
}
