using FluentValidation.Results;
using MediatR;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Domain.Entities;
using ValidationException = VetPlatform.Application.Common.Exceptions.ValidationException;

namespace VetPlatform.Application.Vetheca.Commands.UploadVethecaLibraryDocument;

// Step 1 of "bring your own literature" (see the 2026-09-10 idea in
// docs/VETIA_CLINIC_ANALYSIS.md): extract the text once at upload time and
// store it as searchable chunks, one per PDF page (further split only if a
// page is unusually long). The original PDF bytes are never persisted - only
// the extracted text - so the system never holds a second redistributable
// copy of content someone else owns the rights to. Search/synthesis
// integration (actually using these chunks when answering a question) is a
// separate follow-up; this just gets documents in and stored.
public class UploadVethecaLibraryDocumentCommandHandler : IRequestHandler<UploadVethecaLibraryDocumentCommand, VethecaLibraryDocumentDto>
{
    // PdfPig's page.Text has no hard length cap, and some scanned/dense
    // reference pages can be huge - split those further so no single chunk
    // is too large to usefully pass to the LLM later.
    private const int MaxChunkLength = 2000;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPdfTextExtractor _pdfTextExtractor;

    public UploadVethecaLibraryDocumentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IPdfTextExtractor pdfTextExtractor)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _pdfTextExtractor = pdfTextExtractor;
    }

    public async Task<VethecaLibraryDocumentDto> Handle(UploadVethecaLibraryDocumentCommand request, CancellationToken cancellationToken)
    {
        var clinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no esta asociado a ninguna clinica.");

        IReadOnlyList<string> pageTexts;
        try
        {
            pageTexts = _pdfTextExtractor.ExtractPageTexts(request.FileBytes);
        }
        catch (PdfExtractionException)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.FileBytes), "El archivo no es un PDF válido o está dañado."),
            });
        }

        var document = new VethecaLibraryDocument
        {
            ClinicId = clinicId,
            Title = request.Title.Trim(),
            FileName = request.FileName,
            PageCount = pageTexts.Count,
        };

        for (var pageIndex = 0; pageIndex < pageTexts.Count; pageIndex++)
        {
            var pageNumber = pageIndex + 1;
            foreach (var chunkText in ChunkText(pageTexts[pageIndex]))
            {
                document.Chunks.Add(new VethecaLibraryChunk
                {
                    ClinicId = clinicId,
                    PageNumber = pageNumber,
                    Text = chunkText,
                });
            }
        }

        _dbContext.VethecaLibraryDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VethecaLibraryDocumentDto(document.Id, document.Title, document.FileName, document.PageCount, document.CreatedAtUtc);
    }

    private static IEnumerable<string> ChunkText(string pageText)
    {
        var trimmed = pageText.Trim();
        if (trimmed.Length == 0)
        {
            yield break;
        }

        for (var offset = 0; offset < trimmed.Length; offset += MaxChunkLength)
        {
            yield return trimmed.Substring(offset, Math.Min(MaxChunkLength, trimmed.Length - offset));
        }
    }
}
