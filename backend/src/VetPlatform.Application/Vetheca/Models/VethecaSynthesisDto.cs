using System.Text.Json.Serialization;

namespace VetPlatform.Application.Vetheca.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VethecaCitationSource
{
    PubMed,
    Library,
}

public record VethecaCitationDto
{
    public VethecaCitationSource Source { get; init; } = VethecaCitationSource.PubMed;

    // Set when Source == PubMed.
    public string? Pmid { get; init; }

    // Set when Source == Library - the clinic's own uploaded document.
    public Guid? LibraryDocumentId { get; init; }
    public string? LibraryDocumentTitle { get; init; }
    public int? LibraryPageNumber { get; init; }

    public string Claim { get; init; } = string.Empty;

    // A short excerpt the model claims comes verbatim from the cited source
    // (the PubMed abstract, or the library page). QuoteVerified is computed
    // automatically (see AnthropicLlmClient) by checking it actually appears
    // there - not self-reported by the model. False doesn't necessarily mean
    // the claim is wrong, just that the quoted text couldn't be confirmed
    // verbatim - shown honestly as "no se pudo verificar", never hidden.
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
