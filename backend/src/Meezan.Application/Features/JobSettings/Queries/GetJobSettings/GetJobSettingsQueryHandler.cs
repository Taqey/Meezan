using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.JobSettings.Queries.GetJobSettings;

public class GetJobSettingsQueryHandler : IRequestHandler<GetJobSettingsQuery, Result<Dictionary<string, bool>>>
{
    private readonly IJobSettingRepository _repository;

    public GetJobSettingsQueryHandler(IJobSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Dictionary<string, bool>>> Handle(GetJobSettingsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetAllAsync(cancellationToken);
        return rows.ToDictionary(s => s.JobKey, s => s.Enabled);
    }
}

