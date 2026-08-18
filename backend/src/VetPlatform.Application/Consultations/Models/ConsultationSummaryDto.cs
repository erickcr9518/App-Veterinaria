namespace VetPlatform.Application.Consultations.Models;

public class ConsultationSummaryDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public DateTime ConsultationDateUtc { get; init; }
    public string ReasonForVisit { get; init; } = string.Empty;
    public string VeterinarianName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateOnly? FollowUpDate { get; init; }
}
