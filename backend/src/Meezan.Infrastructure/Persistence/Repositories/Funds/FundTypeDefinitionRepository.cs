using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundTypeDefinitionRepository : IFundTypeDefinitionRepository
{
    private readonly ApplicationDbContext _context;

    public FundTypeDefinitionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundTypeDefinition>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var q = _context.FundTypeDefinitions.AsNoTracking();
        if (!includeDeleted) q = q.Where(t => !t.IsDeleted);
        return await q.OrderBy(t => t.DisplayOrder).ThenBy(t => t.NameAr).ToListAsync(cancellationToken);
    }

    public async Task<FundTypeDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<FundTypeDefinition?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Slug.ToLower() == slug.Trim().ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeDefinitions.AnyAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(FundTypeDefinition definition, CancellationToken cancellationToken = default)
    {
        await _context.FundTypeDefinitions.AddAsync(definition, cancellationToken);
    }

    public void Update(FundTypeDefinition definition)
    {
        _context.FundTypeDefinitions.Update(definition);
    }

    public void Delete(FundTypeDefinition definition)
    {
        _context.FundTypeDefinitions.Remove(definition);
    }

    public async Task<List<FundTypeDefinition>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _context.FundTypeDefinitions.Where(t => idList.Contains(t.Id)).ToListAsync(cancellationToken);
    }
}
