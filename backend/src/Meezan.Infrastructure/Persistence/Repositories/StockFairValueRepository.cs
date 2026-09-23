using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class StockFairValueRepository : IStockFairValueRepository
{
    private readonly ApplicationDbContext _context;

    public StockFairValueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockFairValue?> GetByStockIdAsync(int stockId, CancellationToken ct = default)
    {
        return await _context.StockFairValues
            .Include(fv => fv.Methods)
            .FirstOrDefaultAsync(fv => fv.StockId == stockId, ct);
    }

    public async Task UpsertAsync(
        StockFairValue fairValue,
        IEnumerable<StockFairValueMethod> methods,
        CancellationToken ct = default)
    {
        var existing = await _context.StockFairValues
            .Include(fv => fv.Methods)
            .FirstOrDefaultAsync(fv => fv.StockId == fairValue.StockId, ct);

        if (existing is null)
        {
            fairValue.Methods = methods.ToList();
            await _context.StockFairValues.AddAsync(fairValue, ct);
        }
        else
        {
            existing.FairValue = fairValue.FairValue;
            existing.PriceComparison = fairValue.PriceComparison;
            existing.FairValueDiff = fairValue.FairValueDiff;
            existing.FairValueDiffPct = fairValue.FairValueDiffPct;
            existing.MethodsUsedCount = fairValue.MethodsUsedCount;
            existing.MethodsExcludedCount = fairValue.MethodsExcludedCount;
            existing.Confidence = fairValue.Confidence;
            existing.ComputedAt = fairValue.ComputedAt;

            // Replace methods
            _context.StockFairValueMethods.RemoveRange(existing.Methods);
            var newMethods = methods.Select(m => new StockFairValueMethod
            {
                StockFairValueId = existing.Id,
                MethodName = m.MethodName,
                EstimatedValue = m.EstimatedValue,
                IsOutlier = m.IsOutlier
            }).ToList();
            await _context.StockFairValueMethods.AddRangeAsync(newMethods, ct);
            _context.StockFairValues.Update(existing);
        }

        await _context.SaveChangesAsync(ct);
    }
}
