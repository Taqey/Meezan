using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Meezan.Domain.Services;

namespace Meezan.Application.Features.Funds.Commands.DeleteFundType;

public sealed class DeleteFundTypeCommandHandler : IRequestHandler<DeleteFundTypeCommand, DeleteFundTypeResultDto>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFundTypeCommandHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeOverrideRepository typeOverrideRepo,
        IFundRepository fundRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _typeDefRepo = typeDefRepo;
        _typeOverrideRepo = typeOverrideRepo;
        _fundRepo = fundRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteFundTypeResultDto> Handle(DeleteFundTypeCommand request, CancellationToken cancellationToken)
    {
        var t = await _typeDefRepo.GetByIdAsync(request.Id, cancellationToken);
        if (t == null)
        {
            throw new NotFoundException(nameof(FundTypeDefinition), request.Id);
        }

        var fundCount = await _typeOverrideRepo.CountByDefinitionIdAsync(request.Id, cancellationToken);
        var allFunds = await _fundRepo.GetAllAsync(cancellationToken);
        var enumType = FundTypeCatalog.EnumFor(t.Slug);
        fundCount += allFunds.Count(f => f.Type == enumType);

        if (!request.Confirm && fundCount > 0)
        {
            throw new BadRequestException(
                $"Type has {fundCount} fund(s). Pass confirm=true to delete.",
                new { message = $"Type has {fundCount} fund(s). Pass confirm=true to delete.", fundCount });
        }

        t.IsDeleted = true;
        t.DeletedAt = DateTime.UtcNow;
        t.DeletedReason = $"Deleted by admin. Had {fundCount} fund(s).";
        t.IsVisible = false;
        t.UpdatedAt = DateTime.UtcNow;
        t.UpdatedBy = "admin";
        _typeDefRepo.Update(t);

        var overrides = await _typeOverrideRepo.GetByDefinitionIdAsync(request.Id, cancellationToken);
        _typeOverrideRepo.DeleteRange(overrides);

        await _changeLogRepo.AddAsync(new FundTypeChangeLog
        {
            FundId = null,
            FundTypeDefinitionId = t.Id,
            Action = FundTypeChangeAction.Deleted,
            PerformedBy = "admin",
            Reason = $"Deleted type '{t.NameAr}' (had {fundCount} fund(s))",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteFundTypeResultDto("Type deleted", fundCount);
    }
}
