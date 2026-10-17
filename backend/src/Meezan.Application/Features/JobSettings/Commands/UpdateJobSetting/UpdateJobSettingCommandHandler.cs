using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.JobSettings.Commands.UpdateJobSetting;

public class UpdateJobSettingCommandHandler : IRequestHandler<UpdateJobSettingCommand, Result<UpdateJobSettingResultDto>>
{
    private readonly IJobSettingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateJobSettingCommandHandler(IJobSettingRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UpdateJobSettingResultDto>> Handle(UpdateJobSettingCommand request, CancellationToken cancellationToken)
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

