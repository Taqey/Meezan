using FluentValidation;

namespace Meezan.Application.Features.Funds.Sync.Commands.RunFundPriceSync;

public sealed class RunFundPriceSyncCommandValidator : AbstractValidator<RunFundPriceSyncCommand>
{
    public RunFundPriceSyncCommandValidator()
    {
        RuleFor(x => x.TriggeredBy)
            .NotEmpty()
            .MaximumLength(50);
    }
}
