using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Commands.MoveFundType;

public sealed class MoveFundTypeCommandHandler : IRequestHandler<MoveFundTypeCommand, Result<MoveFundTypeResultDto>>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public MoveFundTypeCommandHandler(
        IFundRepository fundRepo,
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeOverrideRepository typeOverrideRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _fundRepo = fundRepo;
        _typeDefRepo = typeDefRepo;
        _typeOverrideRepo = typeOverrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MoveFundTypeResultDto>> Handle(MoveFundTypeCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null) return Error.NotFound("FundNotFound", "");

        var existing = await _typeOverrideRepo.GetByFundIdAsync(request.FundId, cancellationToken);

        if (request.TargetTypeId.HasValue)
        {
            var targetType = await _typeDefRepo.GetByIdAsync(request.TargetTypeId.Value, cancellationToken);
            if (targetType == null) return Error.Validation("TargetTypeNotFound", "Target type not found");

            if (existing == null)
            {
                await _typeOverrideRepo.AddAsync(new FundTypeOverride
                {
                    FundId = request.FundId,
                    FundTypeDefinitionId = targetType.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin"
                }, cancellationToken);
            }
            else
            {
                existing.FundTypeDefinitionId = targetType.Id;
                existing.CreatedAt = DateTime.UtcNow;
                existing.CreatedBy = "admin";
                _typeOverrideRepo.Update(existing);
            }

            await _changeLogRepo.AddAsync(new FundTypeChangeLog
            {
                FundId = request.FundId,
                FundTypeDefinitionId = targetType.Id,
                Action = FundTypeChangeAction.FundOverridden,
                PerformedBy = "admin",
                Reason = $"Moved fund {fund.Code} to '{targetType.NameAr}'",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new MoveFundTypeResultDto($"Moved to '{targetType.NameAr}'", request.FundId, TargetTypeId: targetType.Id);
        }
        else
        {
            if (existing != null)
            {
                _typeOverrideRepo.Delete(existing);
            }

            await _changeLogRepo.AddAsync(new FundTypeChangeLog
            {
                FundId = request.FundId,
                FundTypeDefinitionId = existing?.FundTypeDefinitionId,
                Action = FundTypeChangeAction.FundOverrideRemoved,
                PerformedBy = "admin",
                Reason = $"Fund {fund.Code} reset to default type",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new MoveFundTypeResultDto("Fund reset to default type", request.FundId, Cleared: existing != null);
        }
    }
}
