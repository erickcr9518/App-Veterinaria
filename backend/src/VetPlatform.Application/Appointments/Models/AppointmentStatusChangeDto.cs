namespace VetPlatform.Application.Appointments.Models;

public class AppointmentStatusChangeDto
{
    public Guid Id { get; init; }
    public string? FromStatus { get; init; }
    public string ToStatus { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public string ChangedByName { get; init; } = string.Empty;
}
