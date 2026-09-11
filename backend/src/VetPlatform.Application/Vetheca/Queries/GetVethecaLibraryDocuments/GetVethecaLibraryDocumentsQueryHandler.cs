using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Queries.GetVethecaLibraryDocuments;

// Library documents are shared clinic resources (like Owners/Patients), not
// private to the uploader - every user sees the whole clinic's library.
public class GetVethecaLibraryDocumentsQueryHandler : IRequestHandler<GetVethecaLibraryDocumentsQuery, IReadOnlyList<VethecaLibraryDocumentDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetVethecaLibraryDocumentsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<VethecaLibraryDocumentDto>> Handle(GetVethecaLibraryDocumentsQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.VethecaLibraryDocuments
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAtUtc)
            .Select(d => new VethecaLibraryDocumentDto(d.Id, d.Title, d.FileName, d.PageCount, d.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
