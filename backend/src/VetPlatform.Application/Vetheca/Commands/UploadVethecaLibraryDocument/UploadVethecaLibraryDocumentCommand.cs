using MediatR;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Commands.UploadVethecaLibraryDocument;

public record UploadVethecaLibraryDocumentCommand(string Title, string FileName, byte[] FileBytes)
    : IRequest<VethecaLibraryDocumentDto>;
