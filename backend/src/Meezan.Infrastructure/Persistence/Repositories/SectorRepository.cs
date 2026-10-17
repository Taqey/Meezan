using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class SectorRepository : ISectorRepository
{
    private readonly ApplicationDbContext _context;

    public SectorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Sector?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Sectors.FindAsync([id], cancellationToken);
    }

    public async Task<List<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sectors
            .AsNoTracking()
            .OrderBy(s => s.NameAr)
            .ToListAsync(cancellationToken);
    }
}
