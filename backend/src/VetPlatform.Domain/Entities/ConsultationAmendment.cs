using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class ConsultationAmendment : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid ConsultationId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PreviousValuesJson { get; set; } = string.Empty;

    public Consultation? Consultation { get; set; }
}
