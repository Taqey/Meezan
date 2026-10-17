using MediatR;
using Meezan.Application.Features.Funds.Classification.Commands.AssignClassificationFund;
using Meezan.Application.Features.Funds.Classification.Commands.BulkAddClassificationFunds;
using Meezan.Application.Features.Funds.Classification.Commands.BulkRemoveClassificationFunds;
using Meezan.Application.Features.Funds.Classification.Commands.CreateClassificationNode;
using Meezan.Application.Features.Funds.Classification.Commands.DeleteClassificationNode;
using Meezan.Application.Features.Funds.Classification.Commands.MergeClassificationNodes;
using Meezan.Application.Features.Funds.Classification.Commands.MoveClassificationNode;
using Meezan.Application.Features.Funds.Classification.Commands.ReclassifyUnclassified;
using Meezan.Application.Features.Funds.Classification.Commands.ResetClassificationFundAssignment;
using Meezan.Application.Features.Funds.Classification.Commands.RestoreClassificationNode;
using Meezan.Application.Features.Funds.Classification.Commands.SeedMigrateClassification;
using Meezan.Application.Features.Funds.Classification.Commands.UpdateClassificationNode;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Classification.Queries.GetAdminClassificationTree;
using Meezan.Application.Features.Funds.Classification.Queries.GetClassificationBackup;
using Meezan.Application.Features.Funds.Classification.Queries.GetClassificationChanges;
using Meezan.Application.Features.Funds.Classification.Queries.GetClassificationDeletePreview;
using Meezan.Application.Features.Funds.Classification.Queries.GetClassificationReview;
using Meezan.Application.Features.Funds.Classification.Queries.GetNodeFunds;
using Meezan.Domain.Entities;
using Meezan.WebApi.Common;
using Meezan.WebApi.Contracts.Classification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Admin;

/// <summary>
/// Admin API for the flexible fund classification tree.
/// Controller delegates all operations to MediatR via ISender.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/funds/classification")]
public class AdminFundClassificationController : ControllerBase
{
    private readonly ISender _sender;

    public AdminFundClassificationController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("seed-migrate")]
    [ProducesResponseType(typeof(ClassificationMigrationReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedMigrate(CancellationToken ct = default)
    {
        var result = await _sender.Send(new SeedMigrateClassificationCommand(), ct);
        return result.ToActionResult();
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(List<AdminClassificationNodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetAdminClassificationTreeQuery(includeDeleted), ct);
        return result.ToActionResult();
    }

    [HttpPost("nodes")]
    [ProducesResponseType(typeof(CreateClassificationNodeResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateNode([FromBody] CreateNodeRequest req, CancellationToken ct = default)
    {
        var cmd = new CreateClassificationNodeCommand
        {
            ParentId = req.ParentId,
            Slug = req.Slug,
            NameAr = req.NameAr,
            NameEn = req.NameEn,
            Icon = req.Icon,
            Color = req.Color,
            DisplayOrder = req.DisplayOrder,
            IsVisible = req.IsVisible,
            RequiresChildSelection = req.RequiresChildSelection,
            ChildrenLabelAr = req.ChildrenLabelAr,
            ChildrenLabelEn = req.ChildrenLabelEn,
            AutoRuleJson = req.AutoRuleJson,
            AutoClassificationEnabled = req.AutoClassificationEnabled,
            FundDisposition = req.FundDisposition
        };

        var result = await _sender.Send(cmd, ct);
        return result.ToActionResult();
    }

    [HttpPatch("nodes/{id:int}")]
    [ProducesResponseType(typeof(AdminClassificationNodeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNode(int id, [FromBody] UpdateNodeRequest req, CancellationToken ct = default)
    {
        var cmd = new UpdateClassificationNodeCommand
        {
            Id = id,
            Slug = req.Slug,
            NameAr = req.NameAr,
            NameEn = req.NameEn,
            Icon = req.Icon,
            Color = req.Color,
            DisplayOrder = req.DisplayOrder,
            IsVisible = req.IsVisible,
            RequiresChildSelection = req.RequiresChildSelection,
            ChildrenLabelAr = req.ChildrenLabelAr,
            ChildrenLabelEn = req.ChildrenLabelEn,
            AutoRuleJson = req.AutoRuleJson,
            AutoClassificationEnabled = req.AutoClassificationEnabled
        };

        var result = await _sender.Send(cmd, ct);
        return result.ToActionResult();
    }

    [HttpPost("nodes/{id:int}/move")]
    [ProducesResponseType(typeof(AdminClassificationNodeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveNode(int id, [FromBody] MoveNodeRequest req, CancellationToken ct = default)
    {
        var result = await _sender.Send(new MoveClassificationNodeCommand(id, req.NewParentId), ct);
        return result.ToActionResult();
    }

    [HttpPost("nodes/merge")]
    [ProducesResponseType(typeof(MergeClassificationNodesResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MergeNodes([FromBody] MergeNodesRequest req, CancellationToken ct = default)
    {
        var result = await _sender.Send(new MergeClassificationNodesCommand(req.SourceId, req.TargetId), ct);
        return result.ToActionResult();
    }

    [HttpGet("nodes/{id:int}/delete-preview")]
    [ProducesResponseType(typeof(ClassificationDeletePreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePreview(int id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetClassificationDeletePreviewQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpDelete("nodes/{id:int}")]
    [ProducesResponseType(typeof(DeleteClassificationNodeResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteNode(
        int id,
        [FromQuery] bool confirm = false,
        [FromQuery] int? moveFundsToNodeId = null,
        [FromQuery] bool resetFundsToAuto = false,
        [FromQuery] int? moveChildrenToNodeId = null,
        CancellationToken ct = default)
    {
        var cmd = new DeleteClassificationNodeCommand(id, confirm, moveFundsToNodeId, resetFundsToAuto, moveChildrenToNodeId);
        var result = await _sender.Send(cmd, ct);
        return result.ToActionResult();
    }

    [HttpPost("nodes/{id:int}/restore")]
    [ProducesResponseType(typeof(AdminClassificationNodeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RestoreNode(int id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new RestoreClassificationNodeCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("nodes/{id:int}/funds")]
    [ProducesResponseType(typeof(GetNodeFundsResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNodeFunds(
        int id,
        [FromQuery] string? search,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetNodeFundsQuery(id, search, source, page, pageSize), ct);
        return result.ToActionResult();
    }

    [HttpPost("nodes/{id:int}/funds/bulk")]
    [ProducesResponseType(typeof(BulkAddFundsResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkAddFunds(int id, [FromBody] BulkFundIdsRequest req, CancellationToken ct = default)
    {
        var result = await _sender.Send(new BulkAddClassificationFundsCommand(id, req.FundIds), ct);
        return result.ToActionResult();
    }

    [HttpDelete("nodes/{id:int}/funds/bulk")]
    [ProducesResponseType(typeof(BulkRemoveFundsResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkRemoveFunds(int id, [FromBody] BulkRemoveRequest req, CancellationToken ct = default)
    {
        var cmd = new BulkRemoveClassificationFundsCommand(id, req.FundIds, req.DestinationNodeId, req.ResetToAuto);
        var result = await _sender.Send(cmd, ct);
        return result.ToActionResult();
    }

    [HttpPost("funds/{fundId:int}/assign")]
    [ProducesResponseType(typeof(AssignClassificationFundResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignFund(int fundId, [FromBody] AssignFundRequest req, CancellationToken ct = default)
    {
        var result = await _sender.Send(new AssignClassificationFundCommand(fundId, req.NodeId, req.Reason), ct);
        return result.ToActionResult();
    }

    [HttpDelete("funds/{fundId:int}/assign")]
    [ProducesResponseType(typeof(ResetClassificationFundAssignmentResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetFundAssign(int fundId, CancellationToken ct = default)
    {
        var result = await _sender.Send(new ResetClassificationFundAssignmentCommand(fundId), ct);
        return result.ToActionResult();
    }

    [HttpGet("review")]
    [ProducesResponseType(typeof(ClassificationReviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReview(CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetClassificationReviewQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPost("reclassify-unclassified")]
    [ProducesResponseType(typeof(ReclassifyResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reclassify(CancellationToken ct = default)
    {
        var result = await _sender.Send(new ReclassifyUnclassifiedCommand(), ct);
        return result.ToActionResult();
    }

    [HttpGet("changes")]
    [ProducesResponseType(typeof(ClassificationChangesResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChanges(
        [FromQuery] int? fundId,
        [FromQuery] int? nodeId,
        [FromQuery] ClassificationChangeAction? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetClassificationChangesQuery(fundId, nodeId, action, page, pageSize), ct);
        return result.ToActionResult();
    }

    [HttpGet("backup")]
    [ProducesResponseType(typeof(List<FundClassificationBackup>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBackup(CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetClassificationBackupQuery(), ct);
        return result.ToActionResult();
    }
}
