using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Features.RemovalCandidates.Commands.ConfirmRemovals;

public sealed class ConfirmRemovalsCommandHandler : IRequestHandler<ConfirmRemovalsCommand, ConfirmRemovalsResultDto>
{
    private readonly IStockRepository _stockRepository;

    public ConfirmRemovalsCommandHandler(IStockRepository stockRepository)
    {
        _stockRepository = stockRepository;
    }

    public async Task<ConfirmRemovalsResultDto> Handle(ConfirmRemovalsCommand request, CancellationToken cancellationToken)
    {
        var tickers = Normalise(request.Tickers);
        if (tickers.Count == 0)
        {
            throw new BadRequestException("No tickers supplied.");
        }

        var confirmed = new List<string>();
        var notFound = new List<string>();

        foreach (var ticker in tickers)
        {
            var stock = await _stockRepository.GetByTickerIncludingInactiveAsync(ticker, cancellationToken);
            if (stock is null)
            {
                notFound.Add(ticker);
                continue;
            }

            if (stock.IsActive)
            {
                await _stockRepository.DeactivateAsync(
                    stock.Id,
                    request.Reason ?? "Confirmed by operator via removal-candidates checklist",
                    cancellationToken);
            }

            confirmed.Add(stock.Ticker);
        }

        return new ConfirmRemovalsResultDto(
            Message: $"Deactivated {confirmed.Count} stock(s). Rows retained with IsActive = false.",
            Confirmed: confirmed,
            NotFound: notFound);
    }

    private static List<string> Normalise(List<string>? tickers) =>
        (tickers ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
}
