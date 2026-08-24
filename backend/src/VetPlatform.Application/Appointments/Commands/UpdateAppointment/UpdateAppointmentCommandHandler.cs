using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Appointments.Common;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Appointments.Commands.UpdateAppointment;

public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public UpdateAppointmentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var clinicId = AppointmentAccess.GetRequiredClinicId(_currentUserService);
        var appointment = await _dbContext.Appointments
            .SingleOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Cita", request.Id);

        AppointmentAccess.EnsureCanManage(_currentUserService, appointment.AssignedVeterinarianUserId);
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

        appointment.PatientId = patient.Id;
        appointment.OwnerId = patient.OwnerId;
        appointment.AssignedVeterinarianUserId = assignedVeterinarianUserId;
        appointment.StartsAtUtc = request.StartsAtUtc;
        appointment.EndsAtUtc = request.EndsAtUtc;
        appointment.VisitType = request.VisitType.Trim();
        appointment.Reason = request.Reason.Trim();
        appointment.Notes = request.Notes?.Trim();
        appointment.ReminderChannel = request.ReminderChannel?.Trim();
        appointment.ReminderNotes = request.ReminderNotes?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
