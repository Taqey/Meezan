using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;
using Meezan.Domain.Services;

namespace Meezan.Application.Features.Funds.Types.Commands.DeleteFundType;

public sealed class DeleteFundTypeCommandHandler : IRequestHandler<DeleteFundTypeCommand, Result<DeleteFundTypeResultDto>>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFundTypeCommandHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeOverrideRepository typeOverrideRepo,
        IFundRepository fundRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _typeDefRepo = typeDefRepo;
        _typeOverrideRepo = typeOverrideRepo;
        _fundRepo = fundRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeleteFundTypeResultDto>> Handle(DeleteFundTypeCommand request, CancellationToken cancellationToken)
    {
        var t = await _typeDefRepo.GetByIdAsync(request.Id, cancellationToken);
        if (t == null)
        {
            return Error.NotFound("FundTypeDefinitionNotFound", "");
        }

        var fundCount = await _typeOverrideRepo.CountByDefinitionIdAsync(request.Id, cancellationToken);
        var allFunds = await _fundRepo.GetAllAsync(cancellationToken);
        var enumType = FundTypeCatalog.EnumFor(t.Slug);
        fundCount += allFunds.Count(f => f.Type == enumType);

        if (!request.Confirm && fundCount > 0)
        {
            return Error.Validation(
                "ConfirmRequired",
                $"Type has {fundCount} fund(s). Pass confirm=true to delete.",
                new { message = $"Type has {fundCount} fund(s). Pass confirm=true to delete.", fundCount });
        }

        t.IsDeleted = true;
        t.DeletedAt = DateTime.UtcNow;
        t.DeletedReason = $"Deleted by admin. Had {fundCount} fund(s).";
        t.IsVisible = false;
        t.UpdatedAt = DateTime.UtcNow;
        t.UpdatedBy = "admin";
        _typeDefRepo.Update(t);

        var overrides = await _typeOverrideRepo.GetByDefinitionIdAsync(request.Id, cancellationToken);
        _typeOverrideRepo.DeleteRange(overrides);

        await _changeLogRepo.AddAsync(new FundTypeChangeLog
        {
            FundId = null,
            FundTypeDefinitionId = t.Id,
            Action = FundTypeChangeAction.Deleted,
            PerformedBy = "admin",
            Reason = $"Deleted type '{t.NameAr}' (had {fundCount} fund(s))",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteFundTypeResultDto("Type deleted", fundCount);
    }
}
