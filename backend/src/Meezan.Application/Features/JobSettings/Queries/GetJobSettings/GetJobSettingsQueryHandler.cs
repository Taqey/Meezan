using MediatR;
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
