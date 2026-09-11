namespace VetPlatform.Application.Vetheca.Models;

public record VethecaLibraryDocumentDto(
    Guid Id,
    string Title,
    string FileName,
    int PageCount,
    DateTime UploadedAtUtc);
