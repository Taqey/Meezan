using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.ResetFundOverrides;

public sealed class ResetFundOverridesCommandValidator : AbstractValidator<ResetFundOverridesCommand>
{
    public ResetFundOverridesCommandValidator()
    {
        RuleFor(x => x.FundId)
            .GreaterThan(0)
            .WithMessage("FundId must be greater than 0.");
    }
}
