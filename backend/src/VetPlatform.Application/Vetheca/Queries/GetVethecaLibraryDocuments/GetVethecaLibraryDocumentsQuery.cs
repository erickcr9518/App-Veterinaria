using MediatR;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.GetVethecaLibraryDocuments;

public record GetVethecaLibraryDocumentsQuery : IRequest<IReadOnlyList<VethecaLibraryDocumentDto>>;
