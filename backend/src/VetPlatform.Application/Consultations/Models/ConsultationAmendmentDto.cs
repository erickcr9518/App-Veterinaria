namespace VetPlatform.Application.Consultations.Models;

public class ConsultationAmendmentDto
{
    public Guid Id { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string PreviousValuesJson { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
}
