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
}
