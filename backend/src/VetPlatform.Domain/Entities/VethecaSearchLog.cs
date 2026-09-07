using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

// Every Vetheca "ask" writes one of these - it's both the audit trail (every
// question, regardless of what the user does with the answer) and, when
// IsSaved is set, the user's own saved-research list. See
// docs/VETIA_CLINIC_ANALYSIS.md section D ("AiInteractionAudit"/"SavedResearch")
// - one table instead of two, since they'd otherwise duplicate the same data.
public class VethecaSearchLog : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? SearchQuery { get; set; }
    public int ArticleCount { get; set; }
    public string? ModelUsed { get; set; }
    public bool? EvidenceSufficient { get; set; }
    public string ResultJson { get; set; } = string.Empty;
    public bool IsSaved { get; set; }
    public string? Title { get; set; }

    // Null until the asking user rates the answer. True = helpful,
    // false = not helpful - real signal on where Vetheca falls short,
    // without Erick having to manually re-test every question himself.
    public bool? Feedback { get; set; }
    public string? FeedbackNote { get; set; }
}
