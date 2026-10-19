using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Infrastructure.Persistence.Repositories.Indices;

public class IndexRepository : IIndexRepository
{
    private readonly ApplicationDbContext _context;

    public IndexRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Index?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Indices.FindAsync([id], cancellationToken);
    }

    public async Task<Index?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .FirstOrDefaultAsync(i => i.Code.ToUpper() == code.Trim().ToUpper(), cancellationToken);
    }

    public async Task<List<Index>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .AnyAsync(i => i.Code.ToUpper() == code.Trim().ToUpper(), cancellationToken);
    }

    public Task UpdateAsync(Index index, CancellationToken cancellationToken = default)
    {
        _context.Indices.Update(index);
        return Task.CompletedTask;
    }

    public async Task<IndexConstituent?> GetConstituentAsync(int stockId, int indexId, CancellationToken cancellationToken = default)
    {
        return await _context.IndexConstituents
            .FirstOrDefaultAsync(ic => ic.StockId == stockId && ic.IndexId == indexId, cancellationToken);
    }

    public async Task<IndexConstituent?> GetConstituentWithIndexAsync(int stockId, int indexId, CancellationToken cancellationToken = default)
    {
        return await _context.IndexConstituents
            .Include(ic => ic.Index)
            .FirstOrDefaultAsync(ic => ic.StockId == stockId && ic.IndexId == indexId, cancellationToken);
    }

    public async Task AddConstituentAsync(IndexConstituent constituent, CancellationToken cancellationToken = default)
    {
        await _context.IndexConstituents.AddAsync(constituent, cancellationToken);
    }

    public void RemoveConstituent(IndexConstituent constituent)
    {
        _context.IndexConstituents.Remove(constituent);
    }

    public async Task ReplaceConstituentsAsync(int indexId, IEnumerable<IndexConstituent> constituents, CancellationToken cancellationToken = default)
    {
        var existing = await _context.IndexConstituents
            .Where(c => c.IndexId == indexId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _context.IndexConstituents.RemoveRange(existing);
        }

        await _context.IndexConstituents.AddRangeAsync(constituents, cancellationToken);
    }

    public async Task<HashSet<int>> GetConstituentStockIdsAsync(int indexId, CancellationToken cancellationToken = default)
    {
        var ids = await _context.IndexConstituents.AsNoTracking()
            .Where(c => c.IndexId == indexId)
            .Select(c => c.StockId)
            .ToListAsync(cancellationToken);
        return new HashSet<int>(ids);
    }

    public async Task<List<IndexSummaryProjection>> GetAllSummariesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .Select(i => new IndexSummaryProjection(
                i.Code,
                i.NameAr,
                i.NameEn,
                i.Description,
                i.Constituents.Count(),
                i.LastUpdated
            ))
            .ToListAsync(cancellationToken);
    }
}
