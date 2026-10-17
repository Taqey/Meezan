using FluentValidation;

namespace Meezan.Application.Features.Funds.Sync.Commands.UpdateSyncSettings;

public sealed class UpdateSyncSettingsCommandValidator : AbstractValidator<UpdateSyncSettingsCommand>
{
    public UpdateSyncSettingsCommandValidator()
    {
        When(x => x.RequestDelaySeconds.HasValue, () =>
        {
            RuleFor(x => x.RequestDelaySeconds!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("RequestDelaySeconds must be non-negative.");
        });

        When(x => x.MaxRetries.HasValue, () =>
        {
            RuleFor(x => x.MaxRetries!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("MaxRetries must be non-negative.");
        });

        When(x => x.RetryBackoffSeconds.HasValue, () =>
        {
            RuleFor(x => x.RetryBackoffSeconds!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("RetryBackoffSeconds must be non-negative.");
        });
    }
}
