using FluentValidation;

namespace Meezan.Application.Features.JobSettings.Commands.UpdateJobSetting;

public class UpdateJobSettingCommandValidator : AbstractValidator<UpdateJobSettingCommand>
{
    public UpdateJobSettingCommandValidator()
    {
        RuleFor(x => x.JobKey)
            .NotEmpty().WithMessage("JobKey is required.")
            .MaximumLength(200).WithMessage("JobKey must not exceed 200 characters.");
    }
}
