using MediatR;

namespace Meezan.Application.Features.JobSettings.Commands.UpdateJobSetting;

public record UpdateJobSettingCommand(string JobKey, bool Enabled) : IRequest<UpdateJobSettingResultDto>;
