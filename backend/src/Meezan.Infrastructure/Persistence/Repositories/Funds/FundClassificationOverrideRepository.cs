using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundClassificationOverrideRepository : IFundClassificationOverrideRepository
{
    private readonly ApplicationDbContext _context;

    public FundClassificationOverrideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundClassificationOverride>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationOverrides
            .Where(o => o.FundId == fundId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FundClassificationOverride>> GetByNodeIdAsync(int nodeId, CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationOverrides
            .Where(o => o.NodeId == nodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<FundClassificationOverride?> GetAsync(int fundId, int nodeId, CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationOverrides
            .FirstOrDefaultAsync(o => o.FundId == fundId && o.NodeId == nodeId, cancellationToken);
    }

    public async Task<int> CountByNodeIdAsync(int nodeId, CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationOverrides
            .AsNoTracking()
            .CountAsync(o => o.NodeId == nodeId, cancellationToken);
    }

    public async Task AddAsync(FundClassificationOverride @override, CancellationToken cancellationToken = default)
    {
        await _context.FundClassificationOverrides.AddAsync(@override, cancellationToken);
    }

    public void Update(FundClassificationOverride @override)
    {
        _context.FundClassificationOverrides.Update(@override);
    }

    public void Delete(FundClassificationOverride @override)
    {
        _context.FundClassificationOverrides.Remove(@override);
    }

    public void DeleteRange(IEnumerable<FundClassificationOverride> overrides)
    {
        _context.FundClassificationOverrides.RemoveRange(overrides);
    }
}
