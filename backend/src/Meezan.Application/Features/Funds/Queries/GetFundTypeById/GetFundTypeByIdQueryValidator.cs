using FluentValidation;

namespace Meezan.Application.Features.Funds.Queries.GetFundTypeById;

public sealed class GetFundTypeByIdQueryValidator : AbstractValidator<GetFundTypeByIdQuery>
{
    public GetFundTypeByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
