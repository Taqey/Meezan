using FluentValidation;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationChanges;

public sealed class GetClassificationChangesQueryValidator : AbstractValidator<GetClassificationChangesQuery>
{
    public GetClassificationChangesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .WithMessage("PageSize must be between 1 and 200.");
    }
}
