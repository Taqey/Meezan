using FluentValidation;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetNodeFunds;

public sealed class GetNodeFundsQueryValidator : AbstractValidator<GetNodeFundsQuery>
{
    public GetNodeFundsQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Node id must be greater than zero.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
