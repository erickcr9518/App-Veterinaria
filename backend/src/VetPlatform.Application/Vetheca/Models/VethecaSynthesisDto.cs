namespace VetPlatform.Application.Vetheca.Models;

public record VethecaCitationDto
{
    public string Pmid { get; init; } = string.Empty;
    public string Claim { get; init; } = string.Empty;

    // A short excerpt the model claims comes verbatim from the cited
    // article's abstract. QuoteVerified is computed automatically (see
    // AnthropicLlmClient) by checking it actually appears there - not
    // self-reported by the model. False doesn't necessarily mean the claim
    // is wrong, just that the quoted text couldn't be confirmed verbatim -
    // shown honestly as "no se pudo verificar", never hidden.
    public string? SupportingExcerpt { get; init; }
    public bool QuoteVerified { get; init; }
}

public record VethecaSynthesisDto
{
    public string ModelUsed { get; init; } = string.Empty;
    public bool EvidenceSufficient { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> KeyFindings { get; init; } = Array.Empty<string>();
    public string? ClinicalApplicability { get; init; }
    public string? Limitations { get; init; }
    public IReadOnlyList<VethecaCitationDto> Citations { get; init; } = Array.Empty<VethecaCitationDto>();
}
