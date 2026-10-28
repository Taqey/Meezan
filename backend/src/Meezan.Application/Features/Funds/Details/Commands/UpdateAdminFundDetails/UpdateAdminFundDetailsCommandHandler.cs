using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Details.Commands.UpdateAdminFundDetails;

public sealed class UpdateAdminFundDetailsCommandHandler : IRequestHandler<UpdateAdminFundDetailsCommand, Result<Fund>>
{
    private readonly IFundRepository _fundRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAdminFundDetailsCommandHandler(
        IFundRepository fundRepo,
        IUnitOfWork unitOfWork)
    {
        _fundRepo = fundRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Fund>> Handle(UpdateAdminFundDetailsCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.Id, cancellationToken);
        if (fund == null)
        {
            return Error.NotFound("FundNotFound", $"الصندوق رقم '{request.Id}' غير موجود.");
        }

        var trimmedNameAr = request.NameAr?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedNameAr))
        {
            return Error.Validation("InvalidNameAr", "الاسم الكامل للصندوق مطلوب.");
        }

        if (trimmedNameAr.Length > 250)
        {
            return Error.Validation("InvalidNameAr", "الاسم الكامل لا يمكن أن يتجاوز 250 حرفاً.");
        }

        var trimmedDisplayName = request.DisplayName?.Trim() ?? string.Empty;
        if (trimmedDisplayName.Length > 250)
        {
            return Error.Validation("InvalidDisplayName", "اسم العرض لا يمكن أن يتجاوز 250 حرفاً.");
        }

        var descriptionAr = request.DescriptionAr ?? string.Empty;
        if (descriptionAr.Length > 10000)
        {
            return Error.Validation("InvalidDescriptionAr", "وصف الصندوق لا يمكن أن يتجاوز 10000 حرف.");
        }

        fund.NameAr = trimmedNameAr;
        fund.DisplayName = trimmedDisplayName;
        fund.DescriptionAr = descriptionAr;
        fund.IsNameManuallyEdited = true;
        fund.IsDescriptionManuallyEdited = true;
        fund.UpdatedAt = DateTime.UtcNow;

        _fundRepo.Update(fund);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return fund;
    }
}

