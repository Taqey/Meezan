using FluentValidation;

namespace Meezan.Application.Features.Indices.Queries.GetIndexConstituents;

public class GetIndexConstituentsQueryValidator : AbstractValidator<GetIndexConstituentsQuery>
{
    public GetIndexConstituentsQueryValidator()
    {
        RuleFor(x => x.IndexCode)
            .NotEmpty().WithMessage("IndexCode is required.")
            .MaximumLength(50);
    }
}
