using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Features.RemovalCandidates.Commands.ReactivateSelected;

public sealed class ReactivateSelectedCommandHandler : IRequestHandler<ReactivateSelectedCommand, ReactivateSelectedResultDto>
{
    private readonly IStockRepository _stockRepository;

    public ReactivateSelectedCommandHandler(IStockRepository stockRepository)
    {
        _stockRepository = stockRepository;
    }

    public async Task<ReactivateSelectedResultDto> Handle(ReactivateSelectedCommand request, CancellationToken cancellationToken)
    {
        var tickers = Normalise(request.Tickers);
        if (tickers.Count == 0)
        {
            throw new BadRequestException("No tickers supplied.");
        }

        var reactivated = new List<string>();
        var notFound = new List<string>();

        foreach (var ticker in tickers)
        {
            var stock = await _stockRepository.GetByTickerIncludingInactiveAsync(ticker, cancellationToken);
            if (stock is null)
            {
                notFound.Add(ticker);
                continue;
            }

            if (!stock.IsActive)
            {
                await _stockRepository.ReactivateAsync(stock.Id, cancellationToken);
            }

            reactivated.Add(stock.Ticker);
        }

        return new ReactivateSelectedResultDto(
            Message: $"Reactivated {reactivated.Count} stock(s) (IsActive = true).",
            Reactivated: reactivated,
            NotFound: notFound);
    }

    private static List<string> Normalise(List<string>? tickers) =>
        (tickers ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
}
