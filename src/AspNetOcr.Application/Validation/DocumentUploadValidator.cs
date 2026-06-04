using AspNetOcr.Application.Contracts;
using FluentValidation;

namespace AspNetOcr.Application.Validation;

public sealed class DocumentUploadValidator : AbstractValidator<DocumentUploadRequest>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
        ".tif",
        ".tiff"
    };

    public DocumentUploadValidator()
    {
        RuleFor(request => request.FileName)
            .NotEmpty()
            .Must(HaveAllowedExtension)
            .WithMessage("Unsupported file type.");

        RuleFor(request => request.Content)
            .NotNull()
            .Must(stream => stream.CanRead)
            .WithMessage("Upload stream must be readable.");

        RuleFor(request => request.CorrelationId)
            .NotEmpty();

        RuleFor(request => request.DeclaredLength)
            .Must(length => length is null or > 0)
            .WithMessage("Uploaded file is empty.")
            .Must(length => length is null or <= 25 * 1024 * 1024)
            .WithMessage("Uploaded file exceeds the Phase 1 size limit.");
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        return AllowedExtensions.Contains(Path.GetExtension(fileName));
    }
}
