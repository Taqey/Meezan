using Meezan.Application.Common.Results;
using MediatR;

namespace Meezan.Application.Features.Scraping.Commands.RefreshSelectedStocks;

/// <summary>
/// Operator-initiated re-scrape of an explicit list of tickers (e.g. checked off
/// in the removal-candidates review checklist). Commits ONLY those stocks via a
/// scoped upsert — every other stock's data is untouched.
/// </summary>
public record RefreshSelectedStocksCommand(
    List<string> Tickers,
    string TriggeredBy = "Manual-RefreshSelected") : IRequest<Result<RefreshSelectedStocksResult>>;
