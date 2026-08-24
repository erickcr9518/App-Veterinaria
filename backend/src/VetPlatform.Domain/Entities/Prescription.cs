using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public static class PrescriptionStatus
{
    public const string Draft = "Draft";
    public const string Finalized = "Finalized";
}

public class Prescription : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid ConsultationId { get; set; }
    public Guid VeterinarianUserId { get; set; }

    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public decimal? WeightKgAtPrescription { get; set; }
    public string? GeneralInstructions { get; set; }
    public string? Warnings { get; set; }

    public string Status { get; set; } = PrescriptionStatus.Draft;
    public DateTime? FinalizedAtUtc { get; set; }
    public Guid? FinalizedByUserId { get; set; }

    public Patient? Patient { get; set; }
    public Consultation? Consultation { get; set; }
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
