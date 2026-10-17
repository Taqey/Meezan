using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundClassificationBackupRepository
{
    Task<bool> HasAnyAsync(CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<FundClassificationBackup> rows, CancellationToken cancellationToken = default);
    Task<List<FundClassificationBackup>> GetAllAsync(CancellationToken cancellationToken = default);
}
