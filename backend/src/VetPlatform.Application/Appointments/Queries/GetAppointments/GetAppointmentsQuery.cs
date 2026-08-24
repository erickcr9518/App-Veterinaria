using MediatR;
using VetPlatform.Application.Appointments.Models;

namespace VetPlatform.Application.Appointments.Queries.GetAppointments;

public record GetAppointmentsQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? PatientId,
    Guid? AssignedVeterinarianUserId,
    string? Status) : IRequest<IReadOnlyList<AppointmentDto>>;
