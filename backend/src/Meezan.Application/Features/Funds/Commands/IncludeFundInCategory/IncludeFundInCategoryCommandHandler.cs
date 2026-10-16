using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.IncludeFundInCategory;

public sealed class IncludeFundInCategoryCommandHandler : IRequestHandler<IncludeFundInCategoryCommand, Unit>
{
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public IncludeFundInCategoryCommandHandler(
        IFundCategoryOverrideRepository overrideRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(IncludeFundInCategoryCommand request, CancellationToken cancellationToken)
    {
        var existing = await _overrideRepo.GetAsync(request.FundId, request.CategoryId, cancellationToken);
        if (existing != null)
        {
            if (existing.Action == OverrideAction.Exclude)
            {
                existing.Action = OverrideAction.Include;
                _overrideRepo.Update(existing);
            }
        }
        else
        {
            await _overrideRepo.AddAsync(new FundCategoryOverride
            {
                FundId = request.FundId,
                CategoryId = request.CategoryId,
                Action = OverrideAction.Include,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin"
            }, cancellationToken);
        }

        await _changeLogRepo.AddAsync(new FundCategoryChangeLog
        {
            FundId = request.FundId,
            CategoryId = request.CategoryId,
            Action = CategoryChangeAction.ManualIncluded,
            PerformedBy = "admin",
            Reason = "Manual include",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
