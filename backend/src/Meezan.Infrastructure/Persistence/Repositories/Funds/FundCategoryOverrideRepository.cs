using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundCategoryOverrideRepository : IFundCategoryOverrideRepository
{
    private readonly ApplicationDbContext _context;

    public FundCategoryOverrideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundCategoryOverride>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategoryOverrides
            .Where(o => o.FundId == fundId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FundCategoryOverride>> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategoryOverrides
            .Where(o => o.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FundCategoryOverride>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundCategoryOverrides
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<FundCategoryOverride?> GetAsync(int fundId, int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategoryOverrides
            .FirstOrDefaultAsync(o => o.FundId == fundId && o.CategoryId == categoryId, cancellationToken);
    }

    public async Task AddAsync(FundCategoryOverride @override, CancellationToken cancellationToken = default)
    {
        await _context.FundCategoryOverrides.AddAsync(@override, cancellationToken);
    }

    public void Update(FundCategoryOverride @override)
    {
        _context.FundCategoryOverrides.Update(@override);
    }

    public void Delete(FundCategoryOverride @override)
    {
        _context.FundCategoryOverrides.Remove(@override);
    }

    public async Task ClearByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        var existing = _context.FundCategoryOverrides.Where(o => o.FundId == fundId);
        _context.FundCategoryOverrides.RemoveRange(existing);
        await Task.CompletedTask;
    }

    public async Task ClearByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var existing = _context.FundCategoryOverrides.Where(o => o.CategoryId == categoryId);
        _context.FundCategoryOverrides.RemoveRange(existing);
        await Task.CompletedTask;
    }
}
