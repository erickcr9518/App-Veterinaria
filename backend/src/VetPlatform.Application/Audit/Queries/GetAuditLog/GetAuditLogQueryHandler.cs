using MediatR;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Audit.Models;
using VetPlatform.Application.Common.Exceptions;
using VetPlatform.Application.Common.Interfaces;
using VetPlatform.Domain.Constants;

namespace VetPlatform.Application.Audit.Queries.GetAuditLog;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, IReadOnlyList<AuditEntryDto>>
{
    private const int MaxEntries = 300;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetAuditLogQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<AuditEntryDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var canReadAll = _currentUserService.HasPermission(PermissionCodes.AuditReadAll);
        var canReadOwn = _currentUserService.HasPermission(PermissionCodes.AuditReadOwn);

        if (!canReadAll && !canReadOwn)
        {
            throw new ForbiddenAccessException("No tienes permiso para ver la bitácora de auditoría.");
        }

        var toUtc = request.ToUtc ?? DateTime.UtcNow;
        var fromUtc = request.FromUtc ?? toUtc.AddDays(-30);
        var ownUserId = canReadAll ? (Guid?)null : _currentUserService.UserId;

        var entries = new List<AuditEntryDto>();

        var owners = await _dbContext.Owners
            .AsNoTracking()
            .Where(o => o.CreatedAtUtc >= fromUtc && o.CreatedAtUtc <= toUtc)
            .Where(o => ownUserId == null || o.CreatedByUserId == ownUserId)
            .Select(o => new { o.Id, o.FullName, o.CreatedAtUtc, o.CreatedByUserId })
            .ToListAsync(cancellationToken);

        entries.AddRange(owners.Select(o => new AuditEntryDto
        {
            Id = o.Id,
            OccurredAtUtc = o.CreatedAtUtc,
            EntityType = "Owner",
            EntityId = o.Id,
            Action = "Propietario registrado",
            Summary = o.FullName,
            PerformedByName = ResolveNamePlaceholder(o.CreatedByUserId),
        }));

        var patients = await _dbContext.Patients
            .AsNoTracking()
            .Where(p => p.CreatedAtUtc >= fromUtc && p.CreatedAtUtc <= toUtc)
            .Where(p => ownUserId == null || p.CreatedByUserId == ownUserId)
            .Select(p => new { p.Id, p.Name, p.Species, p.CreatedAtUtc, p.CreatedByUserId })
            .ToListAsync(cancellationToken);

        entries.AddRange(patients.Select(p => new AuditEntryDto
        {
            Id = p.Id,
            OccurredAtUtc = p.CreatedAtUtc,
            EntityType = "Patient",
            EntityId = p.Id,
            Action = "Paciente registrado",
            Summary = $"{p.Name} ({p.Species})",
            PerformedByName = ResolveNamePlaceholder(p.CreatedByUserId),
        }));

        var consultations = await _dbContext.Consultations
            .AsNoTracking()
            .Include(c => c.Patient)
            .Where(c =>
                (c.CreatedAtUtc >= fromUtc && c.CreatedAtUtc <= toUtc) ||
                (c.FinalizedAtUtc != null && c.FinalizedAtUtc >= fromUtc && c.FinalizedAtUtc <= toUtc))
            .Where(c => ownUserId == null || c.CreatedByUserId == ownUserId || c.FinalizedByUserId == ownUserId)
            .Select(c => new
            {
                c.Id,
                PatientName = c.Patient!.Name,
                c.ReasonForVisit,
                c.CreatedAtUtc,
                c.CreatedByUserId,
                c.FinalizedAtUtc,
                c.FinalizedByUserId,
            })
            .ToListAsync(cancellationToken);

        foreach (var c in consultations)
        {
            if (c.CreatedAtUtc >= fromUtc && c.CreatedAtUtc <= toUtc && (ownUserId == null || c.CreatedByUserId == ownUserId))
            {
                entries.Add(new AuditEntryDto
                {
                    Id = c.Id,
                    OccurredAtUtc = c.CreatedAtUtc,
                    EntityType = "Consultation",
                    EntityId = c.Id,
                    Action = "Consulta creada",
                    Summary = $"{c.PatientName} — {c.ReasonForVisit}",
                    PerformedByName = ResolveNamePlaceholder(c.CreatedByUserId),
                });
            }

            if (c.FinalizedAtUtc is { } finalizedAtUtc && finalizedAtUtc >= fromUtc && finalizedAtUtc <= toUtc
                && (ownUserId == null || c.FinalizedByUserId == ownUserId))
            {
                entries.Add(new AuditEntryDto
                {
                    Id = c.Id,
                    OccurredAtUtc = finalizedAtUtc,
                    EntityType = "Consultation",
                    EntityId = c.Id,
                    Action = "Consulta finalizada",
                    Summary = $"{c.PatientName} — {c.ReasonForVisit}",
                    PerformedByName = ResolveNamePlaceholder(c.FinalizedByUserId),
                });
            }
        }

        var amendments = await _dbContext.ConsultationAmendments
            .AsNoTracking()
            .Include(a => a.Consultation!.Patient)
            .Where(a => a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc)
            .Where(a => ownUserId == null || a.CreatedByUserId == ownUserId)
            .Select(a => new { a.Id, a.Reason, PatientName = a.Consultation!.Patient!.Name, a.CreatedAtUtc, a.CreatedByUserId })
            .ToListAsync(cancellationToken);

        entries.AddRange(amendments.Select(a => new AuditEntryDto
        {
            Id = a.Id,
            OccurredAtUtc = a.CreatedAtUtc,
            EntityType = "ConsultationAmendment",
            EntityId = a.Id,
            Action = "Consulta enmendada",
            Summary = $"{a.PatientName} — {a.Reason}",
            PerformedByName = ResolveNamePlaceholder(a.CreatedByUserId),
        }));

        var prescriptions = await _dbContext.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Where(p =>
                (p.CreatedAtUtc >= fromUtc && p.CreatedAtUtc <= toUtc) ||
                (p.FinalizedAtUtc != null && p.FinalizedAtUtc >= fromUtc && p.FinalizedAtUtc <= toUtc))
            .Where(p => ownUserId == null || p.CreatedByUserId == ownUserId || p.FinalizedByUserId == ownUserId)
            .Select(p => new
            {
                p.Id,
                PatientName = p.Patient!.Name,
                p.CreatedAtUtc,
                p.CreatedByUserId,
                p.FinalizedAtUtc,
                p.FinalizedByUserId,
            })
            .ToListAsync(cancellationToken);

        foreach (var p in prescriptions)
        {
            if (p.CreatedAtUtc >= fromUtc && p.CreatedAtUtc <= toUtc && (ownUserId == null || p.CreatedByUserId == ownUserId))
            {
                entries.Add(new AuditEntryDto
                {
                    Id = p.Id,
                    OccurredAtUtc = p.CreatedAtUtc,
                    EntityType = "Prescription",
                    EntityId = p.Id,
                    Action = "Receta creada",
                    Summary = p.PatientName,
                    PerformedByName = ResolveNamePlaceholder(p.CreatedByUserId),
                });
            }

            if (p.FinalizedAtUtc is { } finalizedAtUtc && finalizedAtUtc >= fromUtc && finalizedAtUtc <= toUtc
                && (ownUserId == null || p.FinalizedByUserId == ownUserId))
            {
                entries.Add(new AuditEntryDto
                {
                    Id = p.Id,
                    OccurredAtUtc = finalizedAtUtc,
                    EntityType = "Prescription",
                    EntityId = p.Id,
                    Action = "Receta finalizada",
                    Summary = p.PatientName,
                    PerformedByName = ResolveNamePlaceholder(p.FinalizedByUserId),
                });
            }
        }

        var appointmentChanges = await _dbContext.AppointmentStatusChanges
            .AsNoTracking()
            .Include(s => s.Appointment!.Patient)
            .Where(s => s.ChangedAtUtc >= fromUtc && s.ChangedAtUtc <= toUtc)
            .Where(s => ownUserId == null || s.ChangedByUserId == ownUserId)
            .Select(s => new
            {
                s.Id,
                s.AppointmentId,
                PatientName = s.Appointment!.Patient!.Name,
                s.FromStatus,
                s.ToStatus,
                s.ChangedAtUtc,
                s.ChangedByUserId,
            })
            .ToListAsync(cancellationToken);

        entries.AddRange(appointmentChanges.Select(s => new AuditEntryDto
        {
            Id = s.Id,
            OccurredAtUtc = s.ChangedAtUtc,
            EntityType = "Appointment",
            EntityId = s.AppointmentId,
            Action = s.FromStatus == null ? "Cita agendada" : "Cita: cambio de estado",
            Summary = s.FromStatus == null
                ? s.PatientName
                : $"{s.PatientName} — {s.FromStatus} → {s.ToStatus}",
            PerformedByName = ResolveNamePlaceholder(s.ChangedByUserId),
        }));

        var userIds = entries
            .Select(e => e.PerformedByName)
            .Where(placeholder => placeholder.StartsWith(UserIdPlaceholderPrefix, StringComparison.Ordinal))
            .Select(placeholder => Guid.Parse(placeholder[UserIdPlaceholderPrefix.Length..]))
            .Distinct()
            .ToArray();

        var names = await _identityService.GetUserFullNamesAsync(userIds);

        return entries
            .Select(e => new AuditEntryDto
            {
                Id = e.Id,
                OccurredAtUtc = e.OccurredAtUtc,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                Action = e.Action,
                Summary = e.Summary,
                PerformedByName = e.PerformedByName.StartsWith(UserIdPlaceholderPrefix, StringComparison.Ordinal)
                    ? names.GetValueOrDefault(Guid.Parse(e.PerformedByName[UserIdPlaceholderPrefix.Length..]), "Usuario")
                    : e.PerformedByName,
            })
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(MaxEntries)
            .ToList();
    }

    private const string UserIdPlaceholderPrefix = "__user__:";

    private static string ResolveNamePlaceholder(Guid? userId)
    {
        return userId is { } id ? $"{UserIdPlaceholderPrefix}{id}" : "Sistema";
    }
}
