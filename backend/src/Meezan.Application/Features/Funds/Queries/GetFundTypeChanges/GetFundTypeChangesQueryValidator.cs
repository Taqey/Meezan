using FluentValidation;

namespace Meezan.Application.Features.Funds.Queries.GetFundTypeChanges;

public sealed class GetFundTypeChangesQueryValidator : AbstractValidator<GetFundTypeChangesQuery>
{
    public GetFundTypeChangesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0);
    }
}
