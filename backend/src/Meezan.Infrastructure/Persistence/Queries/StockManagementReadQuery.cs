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
using Meezan.Application.Features.Stocks.DTOs;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Queries;

public class StockManagementReadQuery : IStockManagementReadQuery
{
    private readonly ApplicationDbContext _context;

    public StockManagementReadQuery(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockManagementItemDto>> SearchStocksAsync(string? query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Stocks.AsNoTracking()
            .Include(s => s.Sector)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            dbQuery = dbQuery.Where(s =>
                s.Ticker.Contains(term) ||
                (s.NameAr != null && s.NameAr.Contains(term)) ||
                (s.NameEn != null && s.NameEn.Contains(term)));
        }

        return await dbQuery
            .OrderBy(s => s.Ticker)
            .Take(50)
            .Select(s => new StockManagementItemDto(
                s.Id,
                s.Ticker,
                s.NameAr,
                s.NameEn,
                s.SectorId,
                s.Sector != null ? s.Sector.NameAr : null,
                s.Sector != null ? s.Sector.NameEn : null,
                s.IsManual,
                s.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SectorPickerItemDto>> GetAllSectorsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sectors
            .AsNoTracking()
            .OrderBy(s => s.NameAr)
            .Select(s => new SectorPickerItemDto(s.Id, s.NameAr, s.NameEn))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<IndexPickerItemDto>> GetAllIndicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .Select(i => new IndexPickerItemDto(i.Id, i.Code, i.NameAr, i.NameEn))
            .ToListAsync(cancellationToken);
    }
}
