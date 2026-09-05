using VetPlatform.Application.Vetheca.Models;

namespace VetPlatform.Application.Common.Interfaces;

public interface ILlmClient
{
    // Returns null when synthesis isn't available (no API key configured, or the
    // call failed) - callers should still show the raw articles in that case.
    Task<VethecaSynthesisDto?> SynthesizeAsync(
        string question,
        IReadOnlyList<PubMedArticleDto> articles,
        CancellationToken cancellationToken);
}
