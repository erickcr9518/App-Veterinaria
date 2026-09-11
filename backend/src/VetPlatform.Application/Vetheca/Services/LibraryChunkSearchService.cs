using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Vetheca.Services;

// A clinic's own uploaded literature (see VethecaLibraryDocument) is small
// compared to PubMed - a handful of manuals/textbooks, not millions of
// records - so a simple in-memory keyword-overlap ranker is enough here.
// No vector store/embeddings dependency needed at this scale; revisit if a
// clinic's library grows large enough to make loading all its chunks
// per-question noticeably slow.
public class LibraryChunkSearchService : ILibraryChunkSearchService
{
    private static readonly Regex WordPattern = new(@"[\p{L}\p{Nd}]{3,}", RegexOptions.Compiled);

    // Common short connector words in both languages a clinic's literature
    // might be in - excluded so they don't drown out the real keywords when
    // scoring chunk relevance.
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "that", "this", "from", "are", "was", "were", "has", "have",
        "que", "los", "las", "del", "con", "por", "para", "una", "uno", "esta", "este", "son", "fue",
    };

    private readonly IApplicationDbContext _dbContext;

    public LibraryChunkSearchService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LibraryChunkMatchDto>> SearchAsync(Guid clinicId, string query, int maxResults, CancellationToken cancellationToken)
    {
        var keywords = Tokenize(query);
        if (keywords.Count == 0)
        {
            return Array.Empty<LibraryChunkMatchDto>();
        }

        var chunks = await _dbContext.VethecaLibraryChunks
            .AsNoTracking()
            .Where(c => c.ClinicId == clinicId)
            .Select(c => new { c.DocumentId, DocumentTitle = c.Document!.Title, c.PageNumber, c.Text })
            .ToListAsync(cancellationToken);

        return chunks
            .Select(c => (Chunk: c, Score: CountMatches(c.Text, keywords)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => new LibraryChunkMatchDto(x.Chunk.DocumentId, x.Chunk.DocumentTitle, x.Chunk.PageNumber, x.Chunk.Text))
            .ToArray();
    }

    private static int CountMatches(string text, HashSet<string> keywords)
    {
        var words = Tokenize(text);
        return keywords.Count(words.Contains);
    }

    private static HashSet<string> Tokenize(string text)
    {
        return WordPattern.Matches(text)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => !StopWords.Contains(w))
            .ToHashSet();
    }
}
