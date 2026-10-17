using FluentValidation;

namespace Meezan.Application.Features.Market.Commands.RefreshMarketSnapshots;

public class RefreshMarketSnapshotsCommandValidator : AbstractValidator<RefreshMarketSnapshotsCommand>
{
    public RefreshMarketSnapshotsCommandValidator()
    {
        RuleFor(x => x.TriggeredBy)
            .NotEmpty().WithMessage("TriggeredBy is required.")
            .MaximumLength(100);
    }
}
