namespace VetPlatform.Application.Prescriptions.Models;

public class PrescriptionSummaryDto
{
    public Guid Id { get; init; }
    public Guid ConsultationId { get; init; }
    public Guid PatientId { get; init; }
    public DateTime IssuedAtUtc { get; init; }
    public string VeterinarianName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<string> ProductNames { get; init; } = Array.Empty<string>();
}
