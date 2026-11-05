using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundSyncLogDetailRepository : IFundSyncLogDetailRepository
{
    private readonly ApplicationDbContext _context;

    public FundSyncLogDetailRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<FundSyncLogDetail> details, CancellationToken cancellationToken = default)
    {
        await _context.FundSyncLogDetails.AddRangeAsync(details, cancellationToken);
    }

    public async Task<List<FundSyncLogDetail>> GetByLogIdAsync(int logId, CancellationToken cancellationToken = default)
    {
        return await _context.FundSyncLogDetails
            .Where(d => d.FundSyncLogId == logId)
            .OrderBy(d => d.FundCode)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
