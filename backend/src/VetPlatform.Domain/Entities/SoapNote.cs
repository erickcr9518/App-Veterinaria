using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class SoapNote : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid ConsultationId { get; set; }

    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }
    public bool GeneratedByAi { get; set; }

    public Consultation? Consultation { get; set; }
}
