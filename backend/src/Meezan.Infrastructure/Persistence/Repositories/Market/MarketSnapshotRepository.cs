using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Market;

public class MarketSnapshotRepository : IMarketSnapshotRepository
{
    private readonly ApplicationDbContext _context;

    public MarketSnapshotRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MarketIndexSnapshot>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MarketIndexSnapshots
            .AsNoTracking()
            .OrderBy(s => s.Kind)
            .ThenBy(s => s.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(MarketIndexSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await _context.MarketIndexSnapshots
            .FirstOrDefaultAsync(s => s.Kind == snapshot.Kind && s.Code == snapshot.Code, cancellationToken);
        if (existing == null)
        {
            await _context.MarketIndexSnapshots.AddAsync(snapshot, cancellationToken);
        }
        else
        {
            existing.MubasherCode = snapshot.MubasherCode;
            existing.Value = snapshot.Value;
            existing.Change = snapshot.Change;
            existing.ChangePct = snapshot.ChangePct;
            existing.SourceDateText = snapshot.SourceDateText;
            existing.Source = snapshot.Source;
            existing.FetchedAt = snapshot.FetchedAt;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
