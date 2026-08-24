using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Appointments.Common;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Appointments.Commands.CreateAppointment;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public CreateAppointmentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var clinicId = AppointmentAccess.GetRequiredClinicId(_currentUserService);
        var assignedVeterinarianUserId = AppointmentAccess.ResolveAssignedVeterinarian(_currentUserService, request.AssignedVeterinarianUserId);

        var patient = await _dbContext.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new NotFoundException("Paciente", request.PatientId);

        if (assignedVeterinarianUserId is { } vetId &&
            !await _identityService.UserBelongsToClinicAsync(vetId, clinicId))
        {
            throw new NotFoundException("Veterinario", vetId);
        }

        var appointment = new Appointment
        {
            ClinicId = clinicId,
            PatientId = patient.Id,
            OwnerId = patient.OwnerId,
            AssignedVeterinarianUserId = assignedVeterinarianUserId,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            VisitType = request.VisitType.Trim(),
            Status = AppointmentStatus.Scheduled,
            Reason = request.Reason.Trim(),
            Notes = request.Notes?.Trim(),
            ReminderChannel = request.ReminderChannel?.Trim(),
            ReminderNotes = request.ReminderNotes?.Trim(),
        };

        _dbContext.Appointments.Add(appointment);
        _dbContext.AppointmentStatusChanges.Add(new AppointmentStatusChange
        {
            ClinicId = clinicId,
            AppointmentId = appointment.Id,
            FromStatus = null,
            ToStatus = AppointmentStatus.Scheduled,
            Reason = "Cita creada",
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = _currentUserService.UserId,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return appointment.Id;
    }
}
