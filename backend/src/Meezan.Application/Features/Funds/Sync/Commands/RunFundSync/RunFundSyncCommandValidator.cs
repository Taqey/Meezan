using FluentValidation;

namespace Meezan.Application.Features.Funds.Sync.Commands.RunFundSync;

public sealed class RunFundSyncCommandValidator : AbstractValidator<RunFundSyncCommand>
{
    public RunFundSyncCommandValidator()
    {
        RuleFor(x => x.TriggeredBy)
            .NotEmpty()
            .WithMessage("TriggeredBy is required.");
    }
}
