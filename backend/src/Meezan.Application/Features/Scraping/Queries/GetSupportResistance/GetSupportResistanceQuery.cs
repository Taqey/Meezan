using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Scraping.DTOs;

namespace Meezan.Application.Features.Scraping.Queries.GetSupportResistance;

public record GetSupportResistanceQuery(string Ticker) : IRequest<Result<SupportResistanceDto>>;
