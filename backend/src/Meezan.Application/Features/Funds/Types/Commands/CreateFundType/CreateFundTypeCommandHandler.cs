using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Commands.CreateFundType;

public sealed class CreateFundTypeCommandHandler : IRequestHandler<CreateFundTypeCommand, Result<FundTypeDefinition>>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFundTypeCommandHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _typeDefRepo = typeDefRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FundTypeDefinition>> Handle(CreateFundTypeCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.NameEn ?? request.NameAr)
            : request.Slug.Trim().ToLowerInvariant();

        if (await _typeDefRepo.ExistsBySlugAsync(slug, cancellationToken))
        {
            return Error.Validation("DuplicateSlug", $"Slug '{slug}' already exists.");
        }

        var now = DateTime.UtcNow;
        var entity = new FundTypeDefinition
        {
            Slug = slug,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Icon = request.Icon ?? string.Empty,
            Color = request.Color ?? string.Empty,
            DisplayOrder = request.DisplayOrder,
            IsVisible = request.IsVisible,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        await _typeDefRepo.AddAsync(entity, cancellationToken);

        await _changeLogRepo.AddAsync(new FundTypeChangeLog
        {
            FundId = null,
            FundTypeDefinitionId = entity.Id,
            Action = FundTypeChangeAction.Created,
            PerformedBy = "admin",
            Reason = $"Created type '{entity.NameAr}'",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity;
    }

    private static string GenerateSlug(string input)
    {
        var s = input.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s)
            if (char.IsLetterOrDigit(ch) || ch == '-') sb.Append(ch);
        return sb.ToString().Trim('-');
    }
}
