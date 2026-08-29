namespace VetPlatform.Application.Prescriptions.Models;

public class PrescriptionDetailDto
{
    public Guid Id { get; init; }
    public Guid ConsultationId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientSpecies { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string VeterinarianName { get; init; } = string.Empty;

    public DateTime IssuedAtUtc { get; init; }
    public decimal? WeightKgAtPrescription { get; init; }
    public string? GeneralInstructions { get; init; }
    public string? Warnings { get; init; }

    public string Status { get; init; } = string.Empty;
    public DateTime? FinalizedAtUtc { get; init; }
    public string? FinalizedByName { get; init; }

    public IReadOnlyList<PrescriptionItemDto> Items { get; init; } = Array.Empty<PrescriptionItemDto>();
}
