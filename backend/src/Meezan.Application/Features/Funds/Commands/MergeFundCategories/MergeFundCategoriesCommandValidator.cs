using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.MergeFundCategories;

public sealed class MergeFundCategoriesCommandValidator : AbstractValidator<MergeFundCategoriesCommand>
{
    public MergeFundCategoriesCommandValidator()
    {
        RuleFor(x => x.SourceCategoryId)
            .GreaterThan(0)
            .WithMessage("SourceCategoryId must be greater than 0.");

        RuleFor(x => x.TargetCategoryId)
            .GreaterThan(0)
            .WithMessage("TargetCategoryId must be greater than 0.");

        RuleFor(x => x)
            .Must(x => x.SourceCategoryId != x.TargetCategoryId)
            .WithMessage("Source and target categories cannot be the same.");
    }
}
