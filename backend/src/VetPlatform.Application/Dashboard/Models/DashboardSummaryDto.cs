namespace VetPlatform.Application.Dashboard.Models;

public class DashboardSummaryDto
{
    public int TodaysAppointmentsCount { get; init; }
    public IReadOnlyList<DashboardAppointmentDto> UpcomingAppointments { get; init; } = Array.Empty<DashboardAppointmentDto>();
    public IReadOnlyList<DashboardDraftItemDto> DraftConsultations { get; init; } = Array.Empty<DashboardDraftItemDto>();
    public IReadOnlyList<DashboardDraftItemDto> DraftPrescriptions { get; init; } = Array.Empty<DashboardDraftItemDto>();
    public IReadOnlyList<DashboardRecentPatientDto> RecentPatients { get; init; } = Array.Empty<DashboardRecentPatientDto>();
}

public class DashboardAppointmentDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public DateTime StartsAtUtc { get; init; }
    public DateTime EndsAtUtc { get; init; }
    public string VisitType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public class DashboardDraftItemDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTime DateUtc { get; init; }
}

public class DashboardRecentPatientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Species { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
