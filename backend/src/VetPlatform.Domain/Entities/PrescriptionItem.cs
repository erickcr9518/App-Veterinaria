using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class PrescriptionItem : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid PrescriptionId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? Concentration { get; set; }
    public string? Presentation { get; set; }
    public string Quantity { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string? Instructions { get; set; }

    public Prescription? Prescription { get; set; }
}
