using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundTypeOverrideRepository : IFundTypeOverrideRepository
{
    private readonly ApplicationDbContext _context;

    public FundTypeOverrideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundTypeOverride?> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeOverrides.FirstOrDefaultAsync(o => o.FundId == fundId, cancellationToken);
    }

    public async Task<List<FundTypeOverride>> GetByDefinitionIdAsync(int definitionId, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeOverrides.Where(o => o.FundTypeDefinitionId == definitionId).ToListAsync(cancellationToken);
    }

    public async Task<List<FundTypeOverride>> GetByFundIdsAsync(IEnumerable<int> fundIds, CancellationToken cancellationToken = default)
    {
        var idList = fundIds.ToList();
        return await _context.FundTypeOverrides.Where(o => idList.Contains(o.FundId)).ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetAllFundOverridesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeOverrides.AsNoTracking().ToDictionaryAsync(o => o.FundId, o => o.FundTypeDefinitionId, cancellationToken);
    }

    public async Task<int> CountByDefinitionIdAsync(int definitionId, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeOverrides.CountAsync(o => o.FundTypeDefinitionId == definitionId, cancellationToken);
    }

    public async Task AddAsync(FundTypeOverride @override, CancellationToken cancellationToken = default)
    {
        await _context.FundTypeOverrides.AddAsync(@override, cancellationToken);
    }

    public void Update(FundTypeOverride @override)
    {
        _context.FundTypeOverrides.Update(@override);
    }

    public void Delete(FundTypeOverride @override)
    {
        _context.FundTypeOverrides.Remove(@override);
    }

    public void DeleteRange(IEnumerable<FundTypeOverride> overrides)
    {
        _context.FundTypeOverrides.RemoveRange(overrides);
    }
}
