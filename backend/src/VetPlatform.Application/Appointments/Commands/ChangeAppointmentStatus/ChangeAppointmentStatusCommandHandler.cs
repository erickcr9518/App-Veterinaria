using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Appointments.Common;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Appointments.Commands.ChangeAppointmentStatus;

public class ChangeAppointmentStatusCommandHandler : IRequestHandler<ChangeAppointmentStatusCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public ChangeAppointmentStatusCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(ChangeAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _dbContext.Appointments
            .SingleOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Cita", request.Id);

        AppointmentAccess.EnsureCanManage(_currentUserService, appointment.AssignedVeterinarianUserId);

        if (appointment.Status == request.Status)
        {
            return;
        }

        var previousStatus = appointment.Status;
        appointment.Status = request.Status;
        _dbContext.AppointmentStatusChanges.Add(new AppointmentStatusChange
        {
            ClinicId = appointment.ClinicId,
            AppointmentId = appointment.Id,
            FromStatus = previousStatus,
            ToStatus = request.Status,
            Reason = request.Reason?.Trim(),
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = _currentUserService.UserId,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
