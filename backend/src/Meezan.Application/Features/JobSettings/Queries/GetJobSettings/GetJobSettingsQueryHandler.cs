using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.JobSettings.Queries.GetJobSettings;

public class GetJobSettingsQueryHandler : IRequestHandler<GetJobSettingsQuery, Dictionary<string, bool>>
{
    private readonly IJobSettingRepository _repository;

    public GetJobSettingsQueryHandler(IJobSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Dictionary<string, bool>> Handle(GetJobSettingsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetAllAsync(cancellationToken);
        return rows.ToDictionary(s => s.JobKey, s => s.Enabled);
    }
}
