namespace VetPlatform.Application.Vetheca.Models;

public record VethecaSavedSearchSummaryDto
{
    public Guid Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public string? Title { get; init; }
    public int ArticleCount { get; init; }
    public bool? EvidenceSufficient { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
