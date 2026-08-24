using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public static class AppointmentStatus
{
    public const string Scheduled = "Scheduled";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
    public const string NoShow = "NoShow";

    public static readonly string[] All = { Scheduled, Confirmed, Cancelled, Completed, NoShow };
}

public class Appointment : BaseAuditableEntity, ITenantEntity
{
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? AssignedVeterinarianUserId { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string VisitType { get; set; } = string.Empty;
    public string Status { get; set; } = AppointmentStatus.Scheduled;
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public DateTime? ReminderSentAtUtc { get; set; }
    public string? ReminderChannel { get; set; }
    public string? ReminderNotes { get; set; }

    public Patient? Patient { get; set; }
    public Owner? Owner { get; set; }
    public ICollection<AppointmentStatusChange> StatusChanges { get; set; } = new List<AppointmentStatusChange>();
}
