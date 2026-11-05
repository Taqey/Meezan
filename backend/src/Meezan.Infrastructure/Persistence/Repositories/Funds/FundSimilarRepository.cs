using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundSimilarRepository : IFundSimilarRepository
{
    private readonly ApplicationDbContext _context;

    public FundSimilarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundSimilar>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        // SimilarFund must be loaded: the detail page renders each similar fund's logo.
        return await _context.FundSimilars
            .Include(s => s.SimilarFund)
            .Where(s => s.FundId == fundId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(int FundId, int SimilarFundId)>> GetAllSimilarLinksAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundSimilars
            .AsNoTracking()
            .Select(s => new ValueTuple<int, int>(s.FundId, s.SimilarFundId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<FundSimilar> similars, CancellationToken cancellationToken = default)
    {
        await _context.FundSimilars.AddRangeAsync(similars, cancellationToken);
    }

    public void ClearByFundId(int fundId)
    {
        var existing = _context.FundSimilars.Where(s => s.FundId == fundId);
        _context.FundSimilars.RemoveRange(existing);
    }
}
