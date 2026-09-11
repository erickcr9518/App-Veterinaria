using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Vetheca.Queries.AskVetheca;

// Vetheca MVP (see docs/VETIA_CLINIC_ANALYSIS.md section J):
// step 1-2 search PubMed for raw articles; step 3 adds an LLM synthesis;
// step 5 (this) persists every ask as a VethecaSearchLog row - it's both
// the audit trail (every question, whether or not the user saves it) and,
// once IsSaved is set via SaveVethecaSearchCommand, the user's saved
// research list. See VethecaSearchLog's own comment for why one table.
public class AskVethecaQueryHandler : IRequestHandler<AskVethecaQuery, AskVethecaResult>
{
    // How many of the clinic's own library chunks to pull in alongside the
    // PubMed articles - kept modest so the prompt doesn't balloon.
    private const int MaxLibraryExcerpts = 5;

    private readonly IPubMedClient _pubMedClient;
    private readonly ILlmClient _llmClient;
    private readonly ILibraryChunkSearchService _libraryChunkSearchService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AskVethecaQueryHandler> _logger;

    public AskVethecaQueryHandler(
        IPubMedClient pubMedClient,
        ILlmClient llmClient,
        ILibraryChunkSearchService libraryChunkSearchService,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<AskVethecaQueryHandler> logger)
    {
        _pubMedClient = pubMedClient;
        _llmClient = llmClient;
        _libraryChunkSearchService = libraryChunkSearchService;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AskVethecaResult> Handle(AskVethecaQuery request, CancellationToken cancellationToken)
    {
        // PubMed's index is almost entirely in English, so a question asked in
        // Spanish (the expected case - this whole app is in Spanish) searched
        // verbatim finds close to nothing. Translate to a search query first;
        // fall back to the raw question if that's unavailable, which keeps
        // today's behavior for English questions and when no LLM is configured.
        var searchQuery = await _llmClient.TranslateToSearchQueryAsync(request.Question, cancellationToken)
            ?? request.Question;

        _logger.LogInformation("Vetheca: pregunta {Question} -> busqueda PubMed {SearchQuery}", request.Question, searchQuery);

        var articles = await _pubMedClient.SearchAsync(searchQuery, request.MaxResults, cancellationToken);

        var clinicId = _currentUserService.ClinicId
            ?? throw new ForbiddenAccessException("El usuario actual no esta asociado a ninguna clinica.");

        // Search the clinic's own uploaded literature with both the original
        // question (likely Spanish) and the translated query (English) -
        // purchased manuals could be in either language.
        var libraryExcerpts = await SearchLibraryAsync(clinicId, request.Question, searchQuery, cancellationToken);

        VethecaSynthesisDto? synthesis = null;
        if (articles.Count > 0 || libraryExcerpts.Count > 0)
        {
            synthesis = await _llmClient.SynthesizeAsync(request.Question, articles, libraryExcerpts, cancellationToken);
        }

        var logEntry = await LogSearchAsync(clinicId, request.Question, searchQuery, articles, synthesis, cancellationToken);

        return new AskVethecaResult(logEntry.Id, articles, synthesis);
    }

    private async Task<IReadOnlyList<LibraryChunkMatchDto>> SearchLibraryAsync(
        Guid clinicId, string question, string searchQuery, CancellationToken cancellationToken)
    {
        var byQuestion = await _libraryChunkSearchService.SearchAsync(clinicId, question, MaxLibraryExcerpts, cancellationToken);
        if (string.Equals(question, searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return byQuestion;
        }

        var bySearchQuery = await _libraryChunkSearchService.SearchAsync(clinicId, searchQuery, MaxLibraryExcerpts, cancellationToken);

        return byQuestion
            .Concat(bySearchQuery)
            .GroupBy(e => (e.DocumentId, e.PageNumber))
            .Select(g => g.First())
            .Take(MaxLibraryExcerpts)
            .ToArray();
    }

    private async Task<VethecaSearchLog> LogSearchAsync(
        Guid clinicId,
        string question,
        string searchQuery,
        IReadOnlyList<PubMedArticleDto> articles,
        VethecaSynthesisDto? synthesis,
        CancellationToken cancellationToken)
    {
        var storedResult = new VethecaStoredResult(articles, synthesis);

        var logEntry = new VethecaSearchLog
        {
            ClinicId = clinicId,
            Question = question,
            SearchQuery = searchQuery,
            ArticleCount = articles.Count,
            ModelUsed = synthesis?.ModelUsed,
            EvidenceSufficient = synthesis?.EvidenceSufficient,
            ResultJson = JsonSerializer.Serialize(storedResult),
            IsSaved = false,
        };

        _dbContext.VethecaSearchLogs.Add(logEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return logEntry;
    }
}
