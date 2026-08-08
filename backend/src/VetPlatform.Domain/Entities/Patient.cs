using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class Patient : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? EstimatedAge { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string? ReproductiveStatus { get; set; }
    public string? Color { get; set; }
    public decimal? CurrentWeightKg { get; set; }
    public string? MicrochipNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? CurrentMedications { get; set; }
    public string? VaccinationStatus { get; set; }
    public string? DewormingStatus { get; set; }
    public string Status { get; set; } = "Activo";

    public Owner? Owner { get; set; }
    public ICollection<PatientWeight> WeightHistory { get; set; } = new List<PatientWeight>();
}
