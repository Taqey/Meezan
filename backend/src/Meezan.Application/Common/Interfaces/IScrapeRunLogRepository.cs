using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IScrapeRunLogRepository
{
    Task<ScrapeRunLog> CreateAsync(ScrapeRunLog log, CancellationToken ct = default);
    Task UpdateAsync(ScrapeRunLog log, CancellationToken ct = default);
    Task<ScrapeRunLog?> GetLatestAsync(CancellationToken ct = default);
}
