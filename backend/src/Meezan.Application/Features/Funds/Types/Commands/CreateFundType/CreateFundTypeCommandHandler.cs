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

namespace Meezan.Application.Features.Funds.Types.Commands.CreateFundType;

public sealed class CreateFundTypeCommandHandler : IRequestHandler<CreateFundTypeCommand, FundTypeDefinition>
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

    public async Task<FundTypeDefinition> Handle(CreateFundTypeCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.NameEn ?? request.NameAr)
            : request.Slug.Trim().ToLowerInvariant();

        if (await _typeDefRepo.ExistsBySlugAsync(slug, cancellationToken))
        {
            throw new BadRequestException($"Slug '{slug}' already exists.");
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
