using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundPriceHistoryRepository
{
    Task<List<FundPriceHistory>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default);
    Task<List<FundPriceHistory>> GetByFundIdAndDateRangeAsync(int fundId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<FundPriceHistory?> GetLatestAsync(int fundId, CancellationToken cancellationToken = default);
    Task<DateTime?> GetMaxDateAsync(int fundId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<FundPriceHistory> history, CancellationToken cancellationToken = default);
    void UpdateRange(IEnumerable<FundPriceHistory> history);
}
