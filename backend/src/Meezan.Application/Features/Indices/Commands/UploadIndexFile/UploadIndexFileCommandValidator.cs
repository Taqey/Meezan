using FluentValidation;

namespace Meezan.Application.Features.Indices.Commands.UploadIndexFile;

public class UploadIndexFileCommandValidator : AbstractValidator<UploadIndexFileCommand>
{
    private static readonly string[] AllowedExtensions = { ".xls", ".xlsx" };

    public UploadIndexFileCommandValidator()
    {
        RuleFor(x => x.IndexCode)
            .NotEmpty().WithMessage("IndexCode is required.")
            .MaximumLength(50).WithMessage("IndexCode must not exceed 50 characters.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.")
            .Must(HaveAllowedExtension).WithMessage("File must have a valid .xls or .xlsx extension.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File is required.")
            .Must(s => s != null && s.Length > 0).WithMessage("File must not be empty.");
    }

    private bool HaveAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(ext);
    }
}
