using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Sectors;

public interface ISectorRepository
{
    Task<Sector?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Sector>> GetAllAsync(CancellationToken cancellationToken = default);
}
