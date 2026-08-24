using MediatR;
using VetPlatform.Application.Appointments.Models;

namespace VetPlatform.Application.Appointments.Queries.GetAppointmentById;

public record GetAppointmentByIdQuery(Guid Id) : IRequest<AppointmentDto>;
