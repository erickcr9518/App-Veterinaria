namespace VetPlatform.Application.Patients.Models;

public class PatientDto
{
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Species { get; init; } = string.Empty;
    public string? Breed { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? EstimatedAge { get; init; }
    public string Sex { get; init; } = string.Empty;
    public string? ReproductiveStatus { get; init; }
    public string? Color { get; init; }
    public decimal? CurrentWeightKg { get; init; }
    public string? MicrochipNumber { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Allergies { get; init; }
    public string? ChronicDiseases { get; init; }
    public string? CurrentMedications { get; init; }
    public string? VaccinationStatus { get; init; }
    public string? DewormingStatus { get; init; }
    public string Status { get; init; } = string.Empty;
}
