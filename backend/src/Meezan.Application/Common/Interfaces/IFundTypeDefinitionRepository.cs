using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundTypeDefinitionRepository
{
    Task<List<FundTypeDefinition>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<FundTypeDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FundTypeDefinition?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(FundTypeDefinition definition, CancellationToken cancellationToken = default);
    void Update(FundTypeDefinition definition);
    void Delete(FundTypeDefinition definition);
    Task<List<FundTypeDefinition>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
