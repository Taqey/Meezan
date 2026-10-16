using FluentValidation;

namespace Meezan.Application.Features.Funds.Queries.GetFundBySlug;

public sealed class GetFundBySlugQueryValidator : AbstractValidator<GetFundBySlugQuery>
{
    public GetFundBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Fund slug is required.")
            .MaximumLength(150)
            .WithMessage("Fund slug cannot exceed 150 characters.");
    }
}
