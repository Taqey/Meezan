using MediatR;
using Meezan.Application.Features.Funds.Commands.BulkAddFundsToType;
using Meezan.Application.Features.Funds.Commands.ClearFundType;
using Meezan.Application.Features.Funds.Commands.CreateFundType;
using Meezan.Application.Features.Funds.Commands.DeleteFundType;
using Meezan.Application.Features.Funds.Commands.MergeFundTypes;
using Meezan.Application.Features.Funds.Commands.MoveFundType;
using Meezan.Application.Features.Funds.Commands.ReorderFundTypes;
using Meezan.Application.Features.Funds.Commands.RestoreFundType;
using Meezan.Application.Features.Funds.Commands.SetFundType;
using Meezan.Application.Features.Funds.Commands.UpdateFundType;
using Meezan.Application.Features.Funds.Queries.GetAdminFundTypes;
using Meezan.Application.Features.Funds.Queries.GetFundTypeById;
using Meezan.Application.Features.Funds.Queries.GetFundTypeChanges;
using Meezan.Application.Features.Funds.Queries.GetTypeFunds;
using Meezan.Domain.Entities;
using Meezan.WebApi.Contracts.AdminFundTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

/// <summary>
/// Editable fund types (الأنواع). Distinct from /categories: a type is the
/// SNDUK classification of the fund itself (equity, fixed income, ...), with
/// operator-editable AR/EN labels, ordering and visibility.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/funds/types")]
public class AdminFundTypesController : ControllerBase
{
    private readonly ISender _sender;

    public AdminFundTypesController(ISender sender)
    {
        _sender = sender;
    }

    // GET /api/admin/funds/types
    [HttpGet]
    [ProducesResponseType(typeof(List<AdminFundTypeItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAdminFundTypesQuery(includeDeleted), ct);
        return Ok(result);
    }

    // GET /api/admin/funds/types/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FundTypeDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var t = await _sender.Send(new GetFundTypeByIdQuery(id), ct);
        return t == null ? NotFound() : Ok(t);
    }

    // POST /api/admin/funds/types
    [HttpPost]
    [ProducesResponseType(typeof(FundTypeDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFundTypeRequest req, CancellationToken ct = default)
    {
        var cmd = new CreateFundTypeCommand
        {
            NameAr = req.NameAr,
            NameEn = req.NameEn,
            Slug = req.Slug,
            Icon = req.Icon,
            Color = req.Color,
            DisplayOrder = req.DisplayOrder,
            IsVisible = req.IsVisible
        };
        var entity = await _sender.Send(cmd, ct);
        return Ok(entity);
    }

    // PATCH /api/admin/funds/types/{id}
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(FundTypeDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFundTypeRequest req, CancellationToken ct = default)
    {
        var cmd = new UpdateFundTypeCommand
        {
            Id = id,
            NameAr = req.NameAr,
            NameEn = req.NameEn,
            Icon = req.Icon,
            Color = req.Color,
            DisplayOrder = req.DisplayOrder,
            IsVisible = req.IsVisible
        };
        var result = await _sender.Send(cmd, ct);
        return Ok(result);
    }

    // DELETE /api/admin/funds/types/{id}?confirm=true
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(DeleteFundTypeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool confirm = false, CancellationToken ct = default)
    {
        var result = await _sender.Send(new DeleteFundTypeCommand(id, confirm), ct);
        return Ok(result);
    }

    // POST /api/admin/funds/types/{id}/restore
    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(typeof(FundTypeDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(int id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new RestoreFundTypeCommand(id), ct);
        return Ok(result);
    }

    // POST /api/admin/funds/types/merge
    [HttpPost("merge")]
    [ProducesResponseType(typeof(MergeFundTypesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Merge([FromBody] MergeFundTypesRequest req, CancellationToken ct = default)
    {
        var cmd = new MergeFundTypesCommand(req.SourceId, req.TargetId);
        var result = await _sender.Send(cmd, ct);
        return Ok(result);
    }

    // POST /api/admin/funds/types/reorder
    [HttpPost("reorder")]
    [ProducesResponseType(typeof(ReorderFundTypesResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reorder([FromBody] ReorderFundTypesRequest req, CancellationToken ct = default)
    {
        var cmd = new ReorderFundTypesCommand(req.OrderedIds);
        var result = await _sender.Send(cmd, ct);
        return Ok(result);
    }

    // GET /api/admin/funds/types/{id}/funds
    [HttpGet("{id:int}/funds")]
    [ProducesResponseType(typeof(TypeFundsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTypeFunds(
        int id,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetTypeFundsQuery(id, search, page, pageSize), ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // POST /api/admin/funds/types/funds/{fundId}/{typeId}
    [HttpPost("funds/{fundId:int}/{typeId:int}")]
    [ProducesResponseType(typeof(SetFundTypeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetFundType(int fundId, int typeId, CancellationToken ct = default)
    {
        var result = await _sender.Send(new SetFundTypeCommand(fundId, typeId), ct);
        return Ok(result);
    }

    // POST /api/admin/funds/types/{typeId}/funds/bulk
    [HttpPost("{typeId:int}/funds/bulk")]
    [ProducesResponseType(typeof(BulkAddFundsToTypeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkAddFundsToType(int typeId, [FromBody] BulkTypeFundRequest req, CancellationToken ct = default)
    {
        var result = await _sender.Send(new BulkAddFundsToTypeCommand(typeId, req.FundIds), ct);
        return Ok(result);
    }

    // POST /api/admin/funds/types/funds/{fundId}/move
    [HttpPost("funds/{fundId:int}/move")]
    [ProducesResponseType(typeof(MoveFundTypeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveFundType(int fundId, [FromBody] MoveFundTypeRequest req, CancellationToken ct = default)
    {
        var result = await _sender.Send(new MoveFundTypeCommand(fundId, req.TargetTypeId), ct);
        return Ok(result);
    }

    // DELETE /api/admin/funds/types/funds/{fundId}
    [HttpDelete("funds/{fundId:int}")]
    [ProducesResponseType(typeof(ClearFundTypeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearFundType(int fundId, CancellationToken ct = default)
    {
        var result = await _sender.Send(new ClearFundTypeCommand(fundId), ct);
        return Ok(result);
    }

    // GET /api/admin/funds/types/changes
    [HttpGet("changes")]
    [ProducesResponseType(typeof(FundTypeChangesResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChanges([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetFundTypeChangesQuery(page, pageSize), ct);
        return Ok(result);
    }
}