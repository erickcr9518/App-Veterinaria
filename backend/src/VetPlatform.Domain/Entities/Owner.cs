using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class Owner : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? IdentificationNumber { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? AlternateContact { get; set; }
    public string? ConsentNotes { get; set; }

    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
