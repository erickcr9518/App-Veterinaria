using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Appointments.Models;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Appointments.Queries.GetAppointmentById;

public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public GetAppointmentByIdQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<AppointmentDto> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Owner)
            .Include(a => a.StatusChanges.OrderByDescending(s => s.ChangedAtUtc))
            .SingleOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Cita", request.Id);

        var userIds = new List<Guid>();
        if (appointment.AssignedVeterinarianUserId is { } veterinarianId)
        {
            userIds.Add(veterinarianId);
        }
        userIds.AddRange(appointment.StatusChanges.Select(s => s.ChangedByUserId).OfType<Guid>());

        var names = await _identityService.GetUserFullNamesAsync(userIds);

        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = appointment.Patient?.Name ?? string.Empty,
            OwnerId = appointment.OwnerId,
            OwnerName = appointment.Owner?.FullName ?? string.Empty,
            AssignedVeterinarianUserId = appointment.AssignedVeterinarianUserId,
            AssignedVeterinarianName = appointment.AssignedVeterinarianUserId is { } vetId ? names.GetValueOrDefault(vetId, "Veterinario") : null,
            StartsAtUtc = appointment.StartsAtUtc,
            EndsAtUtc = appointment.EndsAtUtc,
            VisitType = appointment.VisitType,
            Status = appointment.Status,
            Reason = appointment.Reason,
            Notes = appointment.Notes,
            ReminderSentAtUtc = appointment.ReminderSentAtUtc,
            ReminderChannel = appointment.ReminderChannel,
            ReminderNotes = appointment.ReminderNotes,
            StatusChanges = appointment.StatusChanges.Select(s => new AppointmentStatusChangeDto
            {
                Id = s.Id,
                FromStatus = s.FromStatus,
                ToStatus = s.ToStatus,
                Reason = s.Reason,
                ChangedAtUtc = s.ChangedAtUtc,
                ChangedByName = s.ChangedByUserId is { } changedBy ? names.GetValueOrDefault(changedBy, "Usuario") : "Sistema",
            }).ToList(),
        };
    }
}
