using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Commands.BulkAddClassificationFunds;

public sealed class BulkAddClassificationFundsCommandHandler : IRequestHandler<BulkAddClassificationFundsCommand, BulkAddFundsResultDto>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public BulkAddClassificationFundsCommandHandler(
        IFundClassificationNodeRepository nodeRepo,
        IFundRepository fundRepo,
        IFundClassificationOverrideRepository overrideRepo,
        IFundClassificationChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _nodeRepo = nodeRepo;
        _fundRepo = fundRepo;
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<BulkAddFundsResultDto> Handle(BulkAddClassificationFundsCommand request, CancellationToken cancellationToken)
    {
        var node = await _nodeRepo.GetByIdAsync(request.NodeId, includeDeleted: true, cancellationToken);
        if (node == null)
            throw new NotFoundException("Node not found.");
        if (node.IsDeleted)
            throw new BadRequestException("العقدة محذوفة.");

        int added = 0, failed = 0;
        var errors = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var fid in (request.FundIds ?? new List<int>()).Distinct())
        {
            try
            {
                var fund = await _fundRepo.GetByIdAsync(fid, cancellationToken);
                if (fund == null)
                {
                    failed++;
                    errors.Add($"الصندوق {fid} غير موجود.");
                    continue;
                }

                var manual = await _overrideRepo.GetByFundIdAsync(fid, cancellationToken);
                foreach (var m in manual.Where(m => m.CreatedBy != "AutoSync" && m.Action == ClassificationOverrideAction.Include && m.NodeId != request.NodeId))
                {
                    _overrideRepo.Delete(m);
                }

                var existing = manual.FirstOrDefault(o => o.NodeId == request.NodeId);
                if (existing == null)
                {
                    await _overrideRepo.AddAsync(new FundClassificationOverride
                    {
                        FundId = fid,
                        Node = node,
                        Action = ClassificationOverrideAction.Include,
                        CreatedAt = now,
                        CreatedBy = "admin"
                    }, cancellationToken);
                }
                else if (!string.Equals(existing.CreatedBy, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    existing.CreatedBy = "admin";
                    existing.CreatedAt = now;
                    _overrideRepo.Update(existing);
                }

                await _changeLogRepo.AddAsync(new FundClassificationChangeLog
                {
                    FundId = fid,
                    Node = node,
                    Action = ClassificationChangeAction.FundAssigned,
                    PerformedBy = "admin",
                    Reason = "إضافة جماعية يدوية",
                    CreatedAt = now
                }, cancellationToken);

                added++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{fid}: {ex.Message}");
            }
        }

        await _changeLogRepo.AddAsync(new FundClassificationChangeLog
        {
            FundId = null,
            Node = node,
            Action = ClassificationChangeAction.BulkAssigned,
            PerformedBy = "admin",
            Reason = $"إضافة جماعية: {added} ناجحة، {failed} فاشلة",
            CreatedAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkAddFundsResultDto
        {
            Added = added,
            Failed = failed,
            Errors = errors
        };
    }
}
