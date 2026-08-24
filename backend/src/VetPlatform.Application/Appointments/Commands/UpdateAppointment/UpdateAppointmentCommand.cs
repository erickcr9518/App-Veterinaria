using MediatR;

namespace VetPlatform.Application.Appointments.Commands.UpdateAppointment;

public record UpdateAppointmentCommand(
    Guid Id,
    Guid PatientId,
    Guid? AssignedVeterinarianUserId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string VisitType,
    string Reason,
    string? Notes,
    string? ReminderChannel,
    string? ReminderNotes) : IRequest;
