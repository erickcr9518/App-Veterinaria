using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class AppointmentStatusChange : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid AppointmentId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? ChangedByUserId { get; set; }

    public Appointment? Appointment { get; set; }
}
