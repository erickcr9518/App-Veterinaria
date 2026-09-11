using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Vetheca.Commands.DeleteVethecaLibraryDocument;

public class DeleteVethecaLibraryDocumentCommandHandler : IRequestHandler<DeleteVethecaLibraryDocumentCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteVethecaLibraryDocumentCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteVethecaLibraryDocumentCommand request, CancellationToken cancellationToken)
    {
        // Chunks must be loaded/tracked for the cascade delete configured in
        // VethecaLibraryDocumentConfiguration to mark them Deleted too - the
        // audit interceptor then converts both the document and its chunks
        // from Deleted to a soft-delete (IsDeleted = true), same as every
        // other entity in this app.
        var document = await _dbContext.VethecaLibraryDocuments
            .Include(d => d.Chunks)
            .SingleOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Documento de la biblioteca de Vetheca", request.Id);

        _dbContext.VethecaLibraryDocuments.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
