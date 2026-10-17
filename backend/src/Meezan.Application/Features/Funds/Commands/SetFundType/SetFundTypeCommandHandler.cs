using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.SetFundType;

public sealed class SetFundTypeCommandHandler : IRequestHandler<SetFundTypeCommand, SetFundTypeResultDto>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SetFundTypeCommandHandler(
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

    public async Task<SetFundTypeResultDto> Handle(SetFundTypeCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null) throw new NotFoundException(nameof(Fund), request.FundId);

        var t = await _typeDefRepo.GetByIdAsync(request.TypeId, cancellationToken);
        if (t == null) throw new NotFoundException(nameof(FundTypeDefinition), request.TypeId);

        var existing = await _typeOverrideRepo.GetByFundIdAsync(request.FundId, cancellationToken);
        if (existing == null)
        {
            await _typeOverrideRepo.AddAsync(new FundTypeOverride
            {
                FundId = request.FundId,
                FundTypeDefinitionId = request.TypeId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin"
            }, cancellationToken);
        }
        else
        {
            existing.FundTypeDefinitionId = request.TypeId;
            existing.CreatedAt = DateTime.UtcNow;
            existing.CreatedBy = "admin";
            _typeOverrideRepo.Update(existing);
        }

        await _changeLogRepo.AddAsync(new FundTypeChangeLog
        {
            FundId = request.FundId,
            FundTypeDefinitionId = request.TypeId,
            Action = FundTypeChangeAction.FundOverridden,
            PerformedBy = "admin",
            Reason = $"Fund {fund.Code} → '{t.NameAr}'",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetFundTypeResultDto("Fund type overridden", request.FundId, request.TypeId);
    }
}
