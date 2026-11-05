using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundCategoryRepository : IFundCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public FundCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<FundCategory?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategories
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    public async Task<List<FundCategory>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundCategories.AsNoTracking();
        if (!includeDeleted)
            query = query.Where(c => !c.IsDeleted);
        return await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.NameAr).ToListAsync(cancellationToken);
    }

    public async Task<List<FundCategory>> GetVisibleAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundCategories
            .Where(c => c.IsVisible && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.NameAr)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FundCategory category, CancellationToken cancellationToken = default)
    {
        await _context.FundCategories.AddAsync(category, cancellationToken);
    }

    public void Update(FundCategory category)
    {
        _context.FundCategories.Update(category);
    }

    public void Delete(FundCategory category)
    {
        _context.FundCategories.Remove(category);
    }
}
