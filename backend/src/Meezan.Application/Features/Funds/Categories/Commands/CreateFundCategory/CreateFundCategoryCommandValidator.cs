using FluentValidation;

namespace Meezan.Application.Features.Funds.Categories.Commands.CreateFundCategory;

public sealed class CreateFundCategoryCommandValidator : AbstractValidator<CreateFundCategoryCommand>
{
    public CreateFundCategoryCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty()
            .WithMessage("Arabic name is required.");

        RuleFor(x => x.NameEn)
            .NotEmpty()
            .WithMessage("English name is required.");
    }
}
