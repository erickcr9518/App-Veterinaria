using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class PatientWeight : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public Patient? Patient { get; set; }
}
