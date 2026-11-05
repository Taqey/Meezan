using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundClassificationNodeRepository : IFundClassificationNodeRepository
{
    private readonly ApplicationDbContext _context;

    public FundClassificationNodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundClassificationNode?> GetByIdAsync(int id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationNodes.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        return await query.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<FundClassificationNode?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationNodes.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        return await query.FirstOrDefaultAsync(n => n.Slug.ToLower() == slug.Trim().ToLower(), cancellationToken);
    }

    public async Task<List<FundClassificationNode>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationNodes.AsNoTracking();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        return await query.OrderBy(n => n.Level).ThenBy(n => n.DisplayOrder).ThenBy(n => n.NameAr).ToListAsync(cancellationToken);
    }

    public async Task<List<FundClassificationNode>> GetChildrenAsync(int parentId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationNodes.AsNoTracking().Where(n => n.ParentId == parentId);
        if (includeDeleted) query = query.IgnoreQueryFilters();
        else query = query.Where(n => !n.IsDeleted);
        return await query.OrderBy(n => n.DisplayOrder).ThenBy(n => n.NameAr).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FundClassificationNode node, CancellationToken cancellationToken = default)
    {
        await _context.FundClassificationNodes.AddAsync(node, cancellationToken);
    }

    public void Update(FundClassificationNode node)
    {
        _context.FundClassificationNodes.Update(node);
    }

    public void Delete(FundClassificationNode node)
    {
        _context.FundClassificationNodes.Remove(node);
    }
}
