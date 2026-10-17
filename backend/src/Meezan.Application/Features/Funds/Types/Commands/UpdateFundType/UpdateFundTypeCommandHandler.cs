using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Commands.UpdateFundType;

public sealed class UpdateFundTypeCommandHandler : IRequestHandler<UpdateFundTypeCommand, Result<FundTypeDefinition>>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFundTypeCommandHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _typeDefRepo = typeDefRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FundTypeDefinition>> Handle(UpdateFundTypeCommand request, CancellationToken cancellationToken)
    {
        var t = await _typeDefRepo.GetByIdAsync(request.Id, cancellationToken);
        if (t == null)
        {
            return Error.NotFound("FundTypeDefinitionNotFound", "");
        }

        var oldNameAr = t.NameAr;
        var oldVisible = t.IsVisible;
        var oldOrder = t.DisplayOrder;

        if (request.NameAr != null) t.NameAr = request.NameAr;
        if (request.NameEn != null) t.NameEn = request.NameEn;
        if (request.Icon != null) t.Icon = request.Icon;
        if (request.Color != null) t.Color = request.Color;
        if (request.DisplayOrder.HasValue) t.DisplayOrder = request.DisplayOrder.Value;
        if (request.IsVisible.HasValue) t.IsVisible = request.IsVisible.Value;
        t.UpdatedAt = DateTime.UtcNow;
        t.UpdatedBy = "admin";

        _typeDefRepo.Update(t);

        if (oldNameAr != t.NameAr)
            await _changeLogRepo.AddAsync(new FundTypeChangeLog
            {
                FundId = null,
                FundTypeDefinitionId = t.Id,
                Action = FundTypeChangeAction.Renamed,
                PerformedBy = "admin",
                Reason = $"Renamed '{oldNameAr}' → '{t.NameAr}'",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

        if (oldVisible != t.IsVisible)
            await _changeLogRepo.AddAsync(new FundTypeChangeLog
            {
                FundId = null,
                FundTypeDefinitionId = t.Id,
                Action = FundTypeChangeAction.VisibilityChanged,
                PerformedBy = "admin",
                Reason = t.IsVisible ? "Shown" : "Hidden",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

        if (oldOrder != t.DisplayOrder)
            await _changeLogRepo.AddAsync(new FundTypeChangeLog
            {
                FundId = null,
                FundTypeDefinitionId = t.Id,
                Action = FundTypeChangeAction.Reordered,
                PerformedBy = "admin",
                Reason = $"{oldOrder} → {t.DisplayOrder}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return t;
    }
}
