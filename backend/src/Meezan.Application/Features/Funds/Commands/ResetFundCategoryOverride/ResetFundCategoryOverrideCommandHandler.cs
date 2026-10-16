using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.ResetFundCategoryOverride;

public sealed class ResetFundCategoryOverrideCommandHandler : IRequestHandler<ResetFundCategoryOverrideCommand, Unit>
{
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ResetFundCategoryOverrideCommandHandler(
        IFundCategoryOverrideRepository overrideRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ResetFundCategoryOverrideCommand request, CancellationToken cancellationToken)
    {
        var existing = await _overrideRepo.GetAsync(request.FundId, request.CategoryId, cancellationToken);
        if (existing != null)
        {
            _overrideRepo.Delete(existing);

            await _changeLogRepo.AddAsync(new FundCategoryChangeLog
            {
                FundId = request.FundId,
                CategoryId = request.CategoryId,
                Action = CategoryChangeAction.ResetToAuto,
                PerformedBy = "admin",
                Reason = "Reset to auto",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
