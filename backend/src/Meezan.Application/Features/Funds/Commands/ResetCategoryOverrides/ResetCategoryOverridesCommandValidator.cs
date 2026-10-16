using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.ResetCategoryOverrides;

public sealed class ResetCategoryOverridesCommandValidator : AbstractValidator<ResetCategoryOverridesCommand>
{
    public ResetCategoryOverridesCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("CategoryId must be greater than 0.");
    }
}
