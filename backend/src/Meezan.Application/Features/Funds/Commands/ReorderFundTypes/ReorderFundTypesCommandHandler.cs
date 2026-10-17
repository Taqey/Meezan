using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.ReorderFundTypes;

public sealed class ReorderFundTypesCommandHandler : IRequestHandler<ReorderFundTypesCommand, ReorderFundTypesResultDto>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderFundTypesCommandHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _typeDefRepo = typeDefRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReorderFundTypesResultDto> Handle(ReorderFundTypesCommand request, CancellationToken cancellationToken)
    {
        var ids = request.OrderedIds ?? new List<int>();
        var all = await _typeDefRepo.GetByIdsAsync(ids, cancellationToken);
        var index = 0;
        foreach (var id in ids)
        {
            var t = all.FirstOrDefault(x => x.Id == id);
            if (t == null) continue;
            t.DisplayOrder = ++index;
            t.UpdatedAt = DateTime.UtcNow;
            t.UpdatedBy = "admin";
            _typeDefRepo.Update(t);
        }

        await _changeLogRepo.AddAsync(new FundTypeChangeLog
        {
            FundId = null,
            FundTypeDefinitionId = null,
            Action = FundTypeChangeAction.Reordered,
            PerformedBy = "admin",
            Reason = $"Reordered {ids.Count} type(s)",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReorderFundTypesResultDto(ids.Count);
    }
}
