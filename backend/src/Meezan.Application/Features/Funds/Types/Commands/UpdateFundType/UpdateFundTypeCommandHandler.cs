using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Commands.UpdateFundType;

public sealed class UpdateFundTypeCommandHandler : IRequestHandler<UpdateFundTypeCommand, FundTypeDefinition>
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

    public async Task<FundTypeDefinition> Handle(UpdateFundTypeCommand request, CancellationToken cancellationToken)
    {
        var t = await _typeDefRepo.GetByIdAsync(request.Id, cancellationToken);
        if (t == null)
        {
            throw new NotFoundException(nameof(FundTypeDefinition), request.Id);
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
