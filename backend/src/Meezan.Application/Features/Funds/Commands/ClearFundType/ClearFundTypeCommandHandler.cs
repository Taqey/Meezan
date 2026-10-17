using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.ClearFundType;

public sealed class ClearFundTypeCommandHandler : IRequestHandler<ClearFundTypeCommand, ClearFundTypeResultDto>
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

    public async Task<ClearFundTypeResultDto> Handle(ClearFundTypeCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null) throw new NotFoundException(nameof(Fund), request.FundId);

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
