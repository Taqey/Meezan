using FluentValidation;

namespace Meezan.Application.Features.Funds.Types.Queries.GetTypeFunds;

public sealed class GetTypeFundsQueryValidator : AbstractValidator<GetTypeFundsQuery>
{
    public GetTypeFundsQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0);
    }
}
