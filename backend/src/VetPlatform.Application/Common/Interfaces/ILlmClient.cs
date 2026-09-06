using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Common.Interfaces;

public interface ILlmClient
{
    // PubMed's index is almost entirely in English - a Spanish question
    // searched verbatim finds close to nothing. Returns a concise English
    // search query for the given (likely Spanish) clinical question, or
    // null when unavailable (no API key, call failed) - callers should
    // fall back to searching with the original question in that case.
    Task<string?> TranslateToSearchQueryAsync(string question, CancellationToken cancellationToken);

    // Returns null when synthesis isn't available (no API key configured, or the
    // call failed) - callers should still show the raw articles in that case.
    Task<VethecaSynthesisDto?> SynthesizeAsync(
        string question,
        IReadOnlyList<PubMedArticleDto> articles,
        CancellationToken cancellationToken);
}
