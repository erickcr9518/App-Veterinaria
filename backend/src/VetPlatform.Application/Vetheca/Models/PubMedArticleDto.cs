namespace VetPlatform.Application.Vetheca.Models;

public record PubMedArticleDto
{
    public string Pmid { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public string? Journal { get; init; }
    public string? Year { get; init; }
    public string? AbstractText { get; init; }
    public string Url { get; init; } = string.Empty;

    // From PubMed's own PublicationTypeList - real metadata assigned by NLM
    // indexers, not something we ask the LLM to guess. Null when PubMed
    // didn't tag this article - shown as "no confirmado", never invented.
    public string? StudyType { get; init; }
}
