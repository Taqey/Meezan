using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundClassificationBackupRepository : IFundClassificationBackupRepository
{
    private readonly ApplicationDbContext _context;

    public FundClassificationBackupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasAnyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationBackups.AnyAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<FundClassificationBackup> rows, CancellationToken cancellationToken = default)
    {
        await _context.FundClassificationBackups.AddRangeAsync(rows, cancellationToken);
    }

    public async Task<List<FundClassificationBackup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationBackups.AsNoTracking().OrderBy(b => b.FundCode).ToListAsync(cancellationToken);
    }
}
