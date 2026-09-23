using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Scraping.Queries.GetSupportResistance;

public class GetSupportResistanceQueryHandler
    : IRequestHandler<GetSupportResistanceQuery, SupportResistanceDto?>
{
    private readonly IStockRepository _stockRepo;
    private readonly IStockSupportResistanceRepository _srRepo;

    public GetSupportResistanceQueryHandler(
        IStockRepository stockRepo,
        IStockSupportResistanceRepository srRepo)
    {
        _stockRepo = stockRepo;
        _srRepo = srRepo;
    }

    public async Task<SupportResistanceDto?> Handle(
        GetSupportResistanceQuery request,
        CancellationToken cancellationToken)
    {
        var stock = await _stockRepo.GetByTickerAsync(request.Ticker.ToUpperInvariant(), cancellationToken);
        if (stock is null) return null;

        var sr = await _srRepo.GetByStockIdAsync(stock.Id, cancellationToken);
        if (sr is null) return null;

        return new SupportResistanceDto(
            Ticker: stock.Ticker,
            NameAr: stock.NameAr,
            NameEn: stock.NameEn,
            LastPrice: sr.LastPrice,
            ChangePct: sr.ChangePct,
            Pivot: sr.Pivot,
            R1: sr.R1,
            R2: sr.R2,
            S1: sr.S1,
            S2: sr.S2,
            FetchedAt: sr.FetchedAt
        );
    }
}
