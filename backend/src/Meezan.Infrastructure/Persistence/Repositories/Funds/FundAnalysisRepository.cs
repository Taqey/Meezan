using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundAnalysisRepository : IFundAnalysisRepository
{
    private readonly ApplicationDbContext _context;

    public FundAnalysisRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundAnalysis?> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        // Check the change tracker first: rows Added earlier in the same unit of
        // work are not in the database yet, and adding a second one for the same
        // fund would violate IX_FundAnalyses_FundId on save.
        return _context.FundAnalyses.Local.FirstOrDefault(a => a.FundId == fundId)
            ?? await _context.FundAnalyses
                .FirstOrDefaultAsync(a => a.FundId == fundId, cancellationToken);
    }

    public async Task<List<FundAnalysis>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundAnalyses
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FundAnalysis analysis, CancellationToken cancellationToken = default)
    {
        await _context.FundAnalyses.AddAsync(analysis, cancellationToken);
    }

    public void Update(FundAnalysis analysis)
    {
        _context.FundAnalyses.Update(analysis);
    }
}
