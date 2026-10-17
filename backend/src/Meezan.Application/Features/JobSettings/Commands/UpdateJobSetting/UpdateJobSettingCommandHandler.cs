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
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.JobSettings.Commands.UpdateJobSetting;

public class UpdateJobSettingCommandHandler : IRequestHandler<UpdateJobSettingCommand, UpdateJobSettingResultDto>
{
    private readonly IJobSettingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateJobSettingCommandHandler(IJobSettingRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateJobSettingResultDto> Handle(UpdateJobSettingCommand request, CancellationToken cancellationToken)
    {
        var row = await _repository.GetByJobKeyAsync(request.JobKey, cancellationToken);
        if (row == null)
        {
            row = new JobSetting
            {
                JobKey = request.JobKey,
                Enabled = request.Enabled,
                UpdatedAt = DateTime.UtcNow
            };
            await _repository.AddAsync(row, cancellationToken);
        }
        else
        {
            row.Enabled = request.Enabled;
            row.UpdatedAt = DateTime.UtcNow;
            _repository.Update(row);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateJobSettingResultDto(row.JobKey, row.Enabled);
    }
}
