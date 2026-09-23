using MediatR;
using Meezan.Application.Features.Scraping.Queries.GetMarketData;
using Meezan.Application.Features.Scraping.Queries.GetStocksList;
using Meezan.Application.Features.Scraping.Queries.GetSupportResistance;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/stocks")]
public class StocksController : ControllerBase
{
    private readonly ISender _sender;

    public StocksController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns paginated stocks with filtering and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Meezan.Application.Common.PagedResult<StockListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] string? search,
        [FromQuery] string? indexCode,
        [FromQuery] int? sectorId,
        [FromQuery] string? shariahStatus,
        [FromQuery] string? priceComparison,
        [FromQuery] int? minCompliantSources,
        CancellationToken cancellationToken)
    {
        var query = new GetStocksListQuery(
            page, pageSize, sortBy, sortDir, search, indexCode, sectorId, shariahStatus, priceComparison, minCompliantSources);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns full market data and fair value breakdown for a single stock.
    /// </summary>
    [HttpGet("{ticker}/market-data")]
    [ProducesResponseType(typeof(MarketDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMarketData(
        [FromRoute] string ticker,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMarketDataQuery(ticker), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns support and resistance levels for a single stock.
    /// </summary>
    [HttpGet("{ticker}/support-resistance")]
    [ProducesResponseType(typeof(SupportResistanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupportResistance(
        [FromRoute] string ticker,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSupportResistanceQuery(ticker), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
