using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;

namespace Meezan.Application.Common.Interfaces.Integrations;

public interface ISndukFundClient
{
    Task<SndukFundListResponse?> GetFundListAsync(CancellationToken cancellationToken = default);
    Task<SndukPerformanceResponse?> GetFundPerformanceAsync(long fundId, string timeframe = "1Y", CancellationToken cancellationToken = default);
    Task<SndukAnalysisResponse?> GetFundAnalysisAsync(long fundId, CancellationToken cancellationToken = default);
    Task<SndukCompetingResponse?> GetFundCompetingAsync(long fundId, CancellationToken cancellationToken = default);
}
