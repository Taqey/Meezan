using FluentValidation;

namespace Meezan.Application.Features.Funds.Categories.Commands.UpdateFundCategory;

public sealed class UpdateFundCategoryCommandValidator : AbstractValidator<UpdateFundCategoryCommand>
{
    public UpdateFundCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be greater than 0.");
    }
}
