namespace VetPlatform.Application.Consultations.Models;

public class ConsultationDetailDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid VeterinarianUserId { get; init; }
    public string VeterinarianName { get; init; } = string.Empty;

    public DateTime ConsultationDateUtc { get; init; }
    public string ReasonForVisit { get; init; } = string.Empty;
    public string? HistoryOfPresentIllness { get; init; }
    public string? PhysicalExamFindings { get; init; }

    public decimal? TemperatureCelsius { get; init; }
    public int? HeartRateBpm { get; init; }
    public int? RespiratoryRateRpm { get; init; }
    public decimal? WeightKg { get; init; }

    public string? DiagnosticPlan { get; init; }
    public string? Treatment { get; init; }
    public string? Recommendations { get; init; }
    public DateOnly? FollowUpDate { get; init; }

    public string Status { get; init; } = string.Empty;
    public DateTime? FinalizedAtUtc { get; init; }
    public string? FinalizedByName { get; init; }

    public string? Subjective { get; init; }
    public string? Objective { get; init; }
    public string? Assessment { get; init; }
    public string? Plan { get; init; }

    public IReadOnlyList<ConsultationAmendmentDto> Amendments { get; init; } = Array.Empty<ConsultationAmendmentDto>();
}
