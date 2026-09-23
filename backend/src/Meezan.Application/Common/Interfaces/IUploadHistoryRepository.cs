using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IUploadHistoryRepository
{
    Task AddAsync(UploadHistory uploadHistory, CancellationToken cancellationToken = default);
}
