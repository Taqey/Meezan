using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Commands.ClearFundType;

public sealed class ClearFundTypeCommandHandler : IRequestHandler<ClearFundTypeCommand, Result<ClearFundTypeResultDto>>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ClearFundTypeCommandHandler(
        IFundRepository fundRepo,
        IFundTypeOverrideRepository typeOverrideRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _fundRepo = fundRepo;
        _typeOverrideRepo = typeOverrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClearFundTypeResultDto>> Handle(ClearFundTypeCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null) return Error.NotFound("FundNotFound", "");

        var existing = await _typeOverrideRepo.GetByFundIdAsync(request.FundId, cancellationToken);
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
            Reason = $"Fund {fund.Code} reset to SNDUK type",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ClearFundTypeResultDto("Fund type reset to auto", existing != null);
    }
}
