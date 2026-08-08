using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class Clinic : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LegalId { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string TimeZone { get; set; } = "America/Costa_Rica";
    public bool IsActive { get; set; } = true;
}
