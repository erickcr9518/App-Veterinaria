using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Appointments.Models;
using VetPlatform.Application.Common.Interfaces;

namespace VetPlatform.Application.Appointments.Queries.GetAppointments;

public class GetAppointmentsQueryHandler : IRequestHandler<GetAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IIdentityService _identityService;

    public GetAppointmentsQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    {
        _dbContext = dbContext;
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<AppointmentDto>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Owner)
            .Where(a => a.StartsAtUtc < request.ToUtc && a.EndsAtUtc > request.FromUtc);

        if (request.PatientId is { } patientId)
        {
            query = query.Where(a => a.PatientId == patientId);
        }

        if (request.AssignedVeterinarianUserId is { } veterinarianUserId)
        {
            query = query.Where(a => a.AssignedVeterinarianUserId == veterinarianUserId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(a => a.Status == request.Status.Trim());
        }

        var appointments = await query
            .OrderBy(a => a.StartsAtUtc)
            .ToListAsync(cancellationToken);

        var names = await _identityService.GetUserFullNamesAsync(
            appointments.Select(a => a.AssignedVeterinarianUserId).OfType<Guid>());

        return appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = a.Patient?.Name ?? string.Empty,
            OwnerId = a.OwnerId,
            OwnerName = a.Owner?.FullName ?? string.Empty,
            AssignedVeterinarianUserId = a.AssignedVeterinarianUserId,
            AssignedVeterinarianName = a.AssignedVeterinarianUserId is { } vetId ? names.GetValueOrDefault(vetId, "Veterinario") : null,
            StartsAtUtc = a.StartsAtUtc,
            EndsAtUtc = a.EndsAtUtc,
            VisitType = a.VisitType,
            Status = a.Status,
            Reason = a.Reason,
            Notes = a.Notes,
            ReminderSentAtUtc = a.ReminderSentAtUtc,
            ReminderChannel = a.ReminderChannel,
            ReminderNotes = a.ReminderNotes,
        }).ToList();
    }
}
