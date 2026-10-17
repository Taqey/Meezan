using MediatR;
using Meezan.Application.Features.Scraping.DTOs;

namespace Meezan.Application.Features.Scraping.Queries.GetMarketData;

public record GetMarketDataQuery(string Ticker) : IRequest<MarketDataDto?>;
