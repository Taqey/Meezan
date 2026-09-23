using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class UploadHistoryRepository : IUploadHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public UploadHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UploadHistory uploadHistory, CancellationToken cancellationToken = default)
    {
        await _context.UploadHistories.AddAsync(uploadHistory, cancellationToken);
    }
}
