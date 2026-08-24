using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;
using VetPlatform.Infrastructure.Identity;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.Property(a => a.VisitType).HasMaxLength(80).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(1000);
        builder.Property(a => a.ReminderChannel).HasMaxLength(50);
        builder.Property(a => a.ReminderNotes).HasMaxLength(500);

        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Owner)
            .WithMany()
            .HasForeignKey(a => a.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.AssignedVeterinarianUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ClinicId, a.StartsAtUtc });
        builder.HasIndex(a => new { a.ClinicId, a.PatientId, a.StartsAtUtc });
        builder.HasIndex(a => new { a.ClinicId, a.AssignedVeterinarianUserId, a.StartsAtUtc });
        builder.HasIndex(a => new { a.ClinicId, a.Status });
    }
}
