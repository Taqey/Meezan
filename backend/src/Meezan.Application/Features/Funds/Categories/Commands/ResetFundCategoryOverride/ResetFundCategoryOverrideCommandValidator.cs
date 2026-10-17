using FluentValidation;

namespace Meezan.Application.Features.Funds.Categories.Commands.ResetFundCategoryOverride;

public sealed class ResetFundCategoryOverrideCommandValidator : AbstractValidator<ResetFundCategoryOverrideCommand>
{
    public ResetFundCategoryOverrideCommandValidator()
    {
        RuleFor(x => x.FundId)
            .GreaterThan(0)
            .WithMessage("FundId must be greater than 0.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("CategoryId must be greater than 0.");
    }
}
