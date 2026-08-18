using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public static class ConsultationStatus
{
    public const string Draft = "Draft";
    public const string Finalized = "Finalized";
}

public class Consultation : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid VeterinarianUserId { get; set; }

    public DateTime ConsultationDateUtc { get; set; } = DateTime.UtcNow;
    public string ReasonForVisit { get; set; } = string.Empty;
    public string? HistoryOfPresentIllness { get; set; }
    public string? PhysicalExamFindings { get; set; }

    public decimal? TemperatureCelsius { get; set; }
    public int? HeartRateBpm { get; set; }
    public int? RespiratoryRateRpm { get; set; }
    public decimal? WeightKg { get; set; }

    public string? DiagnosticPlan { get; set; }
    public string? Treatment { get; set; }
    public string? Recommendations { get; set; }
    public DateOnly? FollowUpDate { get; set; }

    public string Status { get; set; } = ConsultationStatus.Draft;
    public DateTime? FinalizedAtUtc { get; set; }
    public Guid? FinalizedByUserId { get; set; }

    public Patient? Patient { get; set; }
    public SoapNote? SoapNote { get; set; }
    public ICollection<ConsultationAmendment> Amendments { get; set; } = new List<ConsultationAmendment>();
}
