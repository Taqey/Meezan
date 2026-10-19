using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundRepository
{
    Task<Fund?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Fund?> GetBySndukIdAsync(long sndukId, CancellationToken cancellationToken = default);
    Task<Fund?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Fund?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<List<Fund>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Fund>> GetAllStaticAsync(CancellationToken cancellationToken = default);
    Task<(List<Fund> Items, int TotalCount)> QueryPagedAsync(FundListFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(Fund fund, CancellationToken cancellationToken = default);
    void Update(Fund fund);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
