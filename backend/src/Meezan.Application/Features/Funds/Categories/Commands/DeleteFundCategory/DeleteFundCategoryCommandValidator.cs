using FluentValidation;

namespace Meezan.Application.Features.Funds.Categories.Commands.DeleteFundCategory;

public sealed class DeleteFundCategoryCommandValidator : AbstractValidator<DeleteFundCategoryCommand>
{
    public DeleteFundCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be greater than 0.");
    }
}
