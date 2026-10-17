using MediatR;
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Application.Features.Funds.Details.Queries.GetFundById;
using Meezan.Application.Features.Funds.Details.Queries.GetFundBySlug;
using Meezan.Application.Features.Funds.Categories.Queries.GetFundCategories;
using Meezan.Application.Features.Funds.Classification.Queries.GetFundClassificationTree;
using Meezan.Application.Features.Funds.Listing.Queries.GetFunds;
using Meezan.Application.Features.Funds.Listing.Queries.GetFundStats;
using Meezan.Application.Features.Funds.Types.Queries.GetFundTypes;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Public;

[ApiController]
[Route("api/funds")]
public class FundsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FundsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search, filter, sort and paginate funds with effective classification & type.
    /// Classification filters (all slugs, never enums): node, type, subType, leaf.
    /// Unknown slugs return an empty 200 result, never a server error.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FundListResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFunds(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? subType,
        [FromQuery] string? leaf,
        [FromQuery] string? node,
        [FromQuery] string? riskLevel,
        [FromQuery] long? assetManagerId,
        [FromQuery] bool? isIslamic,
        [FromQuery] bool? isFeatured,
        [FromQuery] int? categoryId,
        [FromQuery] decimal? minManagementFee,
        [FromQuery] decimal? maxManagementFee,
        [FromQuery] decimal? minInvestment,
        [FromQuery] decimal? maxInvestment,
        [FromQuery] string? sortBy,
        [FromQuery] string sortDir = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFundsQuery
        {
            Search = search,
            Type = type,
            SubType = subType,
            Leaf = leaf,
            Node = node,
            RiskLevel = riskLevel,
            AssetManagerId = assetManagerId,
            IsIslamic = isIslamic,
            IsFeatured = isFeatured,
            CategoryId = categoryId,
            MinManagementFee = minManagementFee,
            MaxManagementFee = maxManagementFee,
            MinInvestment = minInvestment,
            MaxInvestment = maxInvestment,
            SortBy = sortBy,
            SortDir = sortDir,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Editable fund type catalog powering the public dropdown.
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<FundTypeOptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTypes(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFundTypesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Hierarchical classification tree with direct & total fund counts.
    /// </summary>
    [HttpGet("classification-tree")]
    [ProducesResponseType(typeof(List<FundClassificationTreeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClassificationTree(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFundClassificationTreeQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lightweight KPI aggregates for the public funds page header strip.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(FundStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFundStats(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFundStatsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Public list of visible fund categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<FundCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFundCategoriesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Fund detail by integer ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFund(int id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFundByIdQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Fund detail by slug.
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(FundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFundBySlugQuery(slug), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
