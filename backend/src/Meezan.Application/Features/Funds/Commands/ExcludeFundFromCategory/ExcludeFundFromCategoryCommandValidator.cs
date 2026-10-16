using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.ExcludeFundFromCategory;

public sealed class ExcludeFundFromCategoryCommandValidator : AbstractValidator<ExcludeFundFromCategoryCommand>
{
    public ExcludeFundFromCategoryCommandValidator()
    {
        RuleFor(x => x.FundId)
            .GreaterThan(0)
            .WithMessage("FundId must be greater than 0.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("CategoryId must be greater than 0.");
    }
}
