using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.ExcludeFundFromCategory;

public sealed class ExcludeFundFromCategoryCommandHandler : IRequestHandler<ExcludeFundFromCategoryCommand, Unit>
{
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ExcludeFundFromCategoryCommandHandler(
        IFundCategoryOverrideRepository overrideRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ExcludeFundFromCategoryCommand request, CancellationToken cancellationToken)
    {
        var existing = await _overrideRepo.GetAsync(request.FundId, request.CategoryId, cancellationToken);
        if (existing != null)
        {
            if (existing.Action == OverrideAction.Include)
            {
                existing.Action = OverrideAction.Exclude;
                _overrideRepo.Update(existing);
            }
        }
        else
        {
            await _overrideRepo.AddAsync(new FundCategoryOverride
            {
                FundId = request.FundId,
                CategoryId = request.CategoryId,
                Action = OverrideAction.Exclude,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin"
            }, cancellationToken);
        }

        await _changeLogRepo.AddAsync(new FundCategoryChangeLog
        {
            FundId = request.FundId,
            CategoryId = request.CategoryId,
            Action = CategoryChangeAction.ManualExcluded,
            PerformedBy = "admin",
            Reason = "Manual exclude",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
