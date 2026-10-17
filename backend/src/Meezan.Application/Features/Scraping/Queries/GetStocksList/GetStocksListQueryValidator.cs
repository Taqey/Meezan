using FluentValidation;

namespace Meezan.Application.Features.Scraping.Queries.GetStocksList;

public class GetStocksListQueryValidator : AbstractValidator<GetStocksListQuery>
{
    public GetStocksListQueryValidator()
    {
    }
}
