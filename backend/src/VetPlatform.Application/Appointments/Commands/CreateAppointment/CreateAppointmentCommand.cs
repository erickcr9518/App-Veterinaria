using MediatR;

namespace VetPlatform.Application.Appointments.Commands.CreateAppointment;

public record CreateAppointmentCommand(
    Guid PatientId,
    Guid? AssignedVeterinarianUserId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string VisitType,
    string Reason,
    string? Notes,
    string? ReminderChannel,
    string? ReminderNotes) : IRequest<Guid>;
