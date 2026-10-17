using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Commands.RestoreFundType;

public sealed class RestoreFundTypeCommandHandler : IRequestHandler<RestoreFundTypeCommand, Result<FundTypeDefinition>>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreFundTypeCommandHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _typeDefRepo = typeDefRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FundTypeDefinition>> Handle(RestoreFundTypeCommand request, CancellationToken cancellationToken)
    {
        var t = await _typeDefRepo.GetByIdAsync(request.Id, cancellationToken);
        if (t == null)
        {
            return Error.NotFound("FundTypeDefinitionNotFound", "");
        }

        t.IsDeleted = false;
        t.DeletedAt = null;
        t.DeletedReason = string.Empty;
        t.IsVisible = true;
        t.UpdatedAt = DateTime.UtcNow;
        t.UpdatedBy = "admin";
        _typeDefRepo.Update(t);

        await _changeLogRepo.AddAsync(new FundTypeChangeLog
        {
            FundId = null,
            FundTypeDefinitionId = t.Id,
            Action = FundTypeChangeAction.Restored,
            PerformedBy = "admin",
            Reason = $"Restored type '{t.NameAr}'",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return t;
    }
}
