using FluentValidation;

namespace VetPlatform.Application.Vetheca.Commands.UploadVethecaLibraryDocument;

public class UploadVethecaLibraryDocumentCommandValidator : AbstractValidator<UploadVethecaLibraryDocumentCommand>
{
    // Matches the [RequestSizeLimit] on the controller action - checked again
    // here so the limit is enforced even if a caller bypasses the HTTP layer.
    private const int MaxFileSizeBytes = 50 * 1024 * 1024;

    public UploadVethecaLibraryDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.FileBytes)
            .NotEmpty().WithMessage("El archivo está vacío.")
            .Must(bytes => bytes.Length <= MaxFileSizeBytes).WithMessage("El archivo no puede superar los 50 MB.");
    }
}
