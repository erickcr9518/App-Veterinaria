using Microsoft.EntityFrameworkCore;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Clinic> Clinics { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Owner> Owners { get; }
    DbSet<Patient> Patients { get; }
    DbSet<PatientWeight> PatientWeights { get; }
    DbSet<Consultation> Consultations { get; }
    DbSet<SoapNote> SoapNotes { get; }
    DbSet<ConsultationAmendment> ConsultationAmendments { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<AppointmentStatusChange> AppointmentStatusChanges { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<PrescriptionItem> PrescriptionItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
