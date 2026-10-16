using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.RestoreFundCategory;

public sealed class RestoreFundCategoryCommandValidator : AbstractValidator<RestoreFundCategoryCommand>
{
    public RestoreFundCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be greater than 0.");
    }
}
