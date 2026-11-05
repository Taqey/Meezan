using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundSyncSettingsRepository : IFundSyncSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public FundSyncSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundSyncSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundSyncSettings
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Update(FundSyncSettings settings)
    {
        _context.FundSyncSettings.Update(settings);
    }
}
