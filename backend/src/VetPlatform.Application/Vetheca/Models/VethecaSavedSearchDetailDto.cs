namespace VetPlatform.Application.Vetheca.Models;

public record VethecaSavedSearchDetailDto
{
    public Guid Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public string? Title { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<PubMedArticleDto> Articles { get; init; } = Array.Empty<PubMedArticleDto>();
    public VethecaSynthesisDto? Synthesis { get; init; }
}
