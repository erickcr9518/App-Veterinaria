namespace VetPlatform.Application.Appointments.Models;

public class AppointmentDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public Guid OwnerId { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public Guid? AssignedVeterinarianUserId { get; init; }
    public string? AssignedVeterinarianName { get; init; }
    public DateTime StartsAtUtc { get; init; }
    public DateTime EndsAtUtc { get; init; }
    public string VisitType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime? ReminderSentAtUtc { get; init; }
    public string? ReminderChannel { get; init; }
    public string? ReminderNotes { get; init; }
    public IReadOnlyList<AppointmentStatusChangeDto> StatusChanges { get; init; } = Array.Empty<AppointmentStatusChangeDto>();
}
