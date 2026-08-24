using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Application.Dashboard.Models;
using VetPlatform.Domain.Constants;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var upcomingEnd = now.AddDays(3);

        var todaysAppointmentsCount = 0;
        IReadOnlyList<DashboardAppointmentDto> upcomingAppointments = Array.Empty<DashboardAppointmentDto>();

        if (_currentUser.HasPermission(PermissionCodes.AppointmentsRead))
        {
            todaysAppointmentsCount = await _dbContext.Appointments
                .AsNoTracking()
                .Where(a => a.StartsAtUtc < todayEnd && a.EndsAtUtc > todayStart
                    && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
                .CountAsync(cancellationToken);

            var appointments = await _dbContext.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.StartsAtUtc < upcomingEnd && a.EndsAtUtc > now
                    && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
                .OrderBy(a => a.StartsAtUtc)
                .Take(8)
                .ToListAsync(cancellationToken);

            upcomingAppointments = appointments.Select(a => new DashboardAppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient?.Name ?? string.Empty,
                StartsAtUtc = a.StartsAtUtc,
                EndsAtUtc = a.EndsAtUtc,
                VisitType = a.VisitType,
                Status = a.Status,
                Reason = a.Reason,
            }).ToList();
        }

        IReadOnlyList<DashboardDraftItemDto> draftConsultations = Array.Empty<DashboardDraftItemDto>();
        if (_currentUser.HasPermission(PermissionCodes.ConsultationsWrite) && _currentUser.UserId is { } veterinarianUserId)
        {
            var consultations = await _dbContext.Consultations
                .AsNoTracking()
                .Include(c => c.Patient)
                .Where(c => c.Status == ConsultationStatus.Draft && c.VeterinarianUserId == veterinarianUserId)
                .OrderByDescending(c => c.CreatedAtUtc)
                .Take(5)
                .ToListAsync(cancellationToken);

            draftConsultations = consultations.Select(c => new DashboardDraftItemDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                PatientName = c.Patient?.Name ?? string.Empty,
                Summary = c.ReasonForVisit,
                DateUtc = c.ConsultationDateUtc,
            }).ToList();
        }

        IReadOnlyList<DashboardDraftItemDto> draftPrescriptions = Array.Empty<DashboardDraftItemDto>();
        if (_currentUser.HasPermission(PermissionCodes.PrescriptionsWrite) && _currentUser.UserId is { } prescribingUserId)
        {
            var prescriptions = await _dbContext.Prescriptions
                .AsNoTracking()
                .Include(p => p.Patient)
                .Include(p => p.Items)
                .Where(p => p.Status == PrescriptionStatus.Draft && p.VeterinarianUserId == prescribingUserId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Take(5)
                .ToListAsync(cancellationToken);

            draftPrescriptions = prescriptions.Select(p => new DashboardDraftItemDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                PatientName = p.Patient?.Name ?? string.Empty,
                Summary = p.Items.Count > 0 ? string.Join(", ", p.Items.Select(i => i.ProductName)) : "Sin productos",
                DateUtc = p.IssuedAtUtc,
            }).ToList();
        }

        IReadOnlyList<DashboardRecentPatientDto> recentPatients = Array.Empty<DashboardRecentPatientDto>();
        if (_currentUser.HasPermission(PermissionCodes.PatientsRead))
        {
            var patients = await _dbContext.Patients
                .AsNoTracking()
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Take(5)
                .ToListAsync(cancellationToken);

            recentPatients = patients.Select(p => new DashboardRecentPatientDto
            {
                Id = p.Id,
                Name = p.Name,
                Species = p.Species,
                OwnerName = p.Owner?.FullName ?? string.Empty,
                CreatedAtUtc = p.CreatedAtUtc,
            }).ToList();
        }

        return new DashboardSummaryDto
        {
            TodaysAppointmentsCount = todaysAppointmentsCount,
            UpcomingAppointments = upcomingAppointments,
            DraftConsultations = draftConsultations,
            DraftPrescriptions = draftPrescriptions,
            RecentPatients = recentPatients,
        };
    }
}
