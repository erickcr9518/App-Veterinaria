using MediatR;

namespace VetPlatform.Application.Vetheca.Commands.DeleteVethecaLibraryDocument;

public record DeleteVethecaLibraryDocumentCommand(Guid Id) : IRequest;
