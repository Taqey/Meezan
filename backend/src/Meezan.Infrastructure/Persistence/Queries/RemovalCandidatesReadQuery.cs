using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.RemovalCandidates.DTOs;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Queries;

public class RemovalCandidatesReadQuery : IRemovalCandidatesReadQuery
{
    private readonly ApplicationDbContext _context;

    public RemovalCandidatesReadQuery(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RemovalCandidateDto>> GetRemovalCandidatesAsync(CancellationToken cancellationToken = default)
    {
        // 262-ish rows — a single lightweight query, filtered in memory because
        // staleness detection (Arabic date text) cannot be translated to SQL.
        var stocks = await _context.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Include(s => s.MarketData)
            .OrderBy(s => s.Ticker)
            .ToListAsync(cancellationToken);

        var result = new List<RemovalCandidateDto>();
        foreach (var s in stocks)
        {
            string? reason = null;
            if (!s.IsActive)
                reason = "Deactivated";
            else if (s.DataStatus != StockDataStatus.Active)
                reason = "NotFoundOnSource";
            else if (s.MarketData is null)
                reason = "NotFoundOnSource";
            else if (MubasherStaleDetector.IsStale(s.MarketData.SourceLastUpdateText))
                reason = "StaleData";

            if (reason is null) continue;

            result.Add(new RemovalCandidateDto(
                StockId: s.Id,
                Ticker: s.Ticker,
                NameAr: s.NameAr,
                NameEn: s.NameEn,
                LastSuccessfulUpdate: s.MarketData?.FetchedAt,
                SourceLastUpdateText: s.MarketData?.SourceLastUpdateText,
                LastClosingPrice: s.MarketData?.ClosingPrice,
                Reason: reason,
                CurrentIsActive: s.IsActive,
                DataStatus: s.DataStatus.ToString(),
                DeactivatedAt: s.DeactivatedAt,
                DeactivationReason: s.DeactivationReason,
                SectorNameAr: s.Sector?.NameAr));
        }

        return result;
    }
}
