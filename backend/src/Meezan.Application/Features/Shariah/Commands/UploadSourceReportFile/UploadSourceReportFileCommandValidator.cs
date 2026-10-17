using FluentValidation;

namespace Meezan.Application.Features.Shariah.Commands.UploadSourceReportFile;

public sealed class UploadSourceReportFileCommandValidator : AbstractValidator<UploadSourceReportFileCommand>
{
    public UploadSourceReportFileCommandValidator()
    {
        RuleFor(x => x.SourceKey).NotEmpty();
        RuleFor(x => x.FileStream).NotNull();
    }
}
