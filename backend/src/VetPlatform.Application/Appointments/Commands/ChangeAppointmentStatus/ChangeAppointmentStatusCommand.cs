using MediatR;

namespace VetPlatform.Application.Appointments.Commands.ChangeAppointmentStatus;

public record ChangeAppointmentStatusCommand(Guid Id, string Status, string? Reason) : IRequest;
