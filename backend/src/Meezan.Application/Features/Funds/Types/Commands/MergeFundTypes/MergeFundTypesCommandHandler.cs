using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Commands.MergeFundTypes;

public sealed class MergeFundTypesCommandHandler : IRequestHandler<MergeFundTypesCommand, Result<MergeFundTypesResultDto>>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public MergeFundTypesCommandHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeOverrideRepository typeOverrideRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _typeDefRepo = typeDefRepo;
        _typeOverrideRepo = typeOverrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MergeFundTypesResultDto>> Handle(MergeFundTypesCommand request, CancellationToken cancellationToken)
    {
        var source = await _typeDefRepo.GetByIdAsync(request.SourceId, cancellationToken);
        var target = await _typeDefRepo.GetByIdAsync(request.TargetId, cancellationToken);
        if (source == null || target == null)
        {
            return Error.NotFound("FundTypeNotFound", "One or both fund types not found.");
        }

        var moved = await _typeOverrideRepo.GetByDefinitionIdAsync(source.Id, cancellationToken);
        foreach (var o in moved)
        {
            o.FundTypeDefinitionId = target.Id;
            o.CreatedBy = "admin";
            _typeOverrideRepo.Update(o);
        }

        source.IsDeleted = true;
        source.DeletedAt = DateTime.UtcNow;
        source.IsVisible = false;
        source.DeletedReason = $"Merged into '{target.NameAr}'";
        source.UpdatedAt = DateTime.UtcNow;
        source.UpdatedBy = "admin";
        _typeDefRepo.Update(source);

        await _changeLogRepo.AddAsync(new FundTypeChangeLog
        {
            FundId = null,
            FundTypeDefinitionId = target.Id,
            Action = FundTypeChangeAction.Merged,
            PerformedBy = "admin",
            Reason = $"Merged '{source.NameAr}' into '{target.NameAr}' ({moved.Count} overrides moved)",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MergeFundTypesResultDto("Types merged", moved.Count);
    }
}
