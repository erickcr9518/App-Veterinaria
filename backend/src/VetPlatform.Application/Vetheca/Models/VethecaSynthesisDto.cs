namespace VetPlatform.Application.Vetheca.Models;

public record VethecaCitationDto
{
    public string Pmid { get; init; } = string.Empty;
    public string Claim { get; init; } = string.Empty;
}

public record VethecaSynthesisDto
{
    public bool EvidenceSufficient { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> KeyFindings { get; init; } = Array.Empty<string>();
    public string? ClinicalApplicability { get; init; }
    public string? Limitations { get; init; }
    public IReadOnlyList<VethecaCitationDto> Citations { get; init; } = Array.Empty<VethecaCitationDto>();
}
