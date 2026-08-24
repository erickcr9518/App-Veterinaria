using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class AppointmentStatusChangeConfiguration : IEntityTypeConfiguration<AppointmentStatusChange>
{
    public void Configure(EntityTypeBuilder<AppointmentStatusChange> builder)
    {
        builder.ToTable("AppointmentStatusChanges");

        builder.Property(s => s.FromStatus).HasMaxLength(30);
        builder.Property(s => s.ToStatus).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Reason).HasMaxLength(1000);

        builder.HasOne(s => s.Appointment)
            .WithMany(a => a.StatusChanges)
            .HasForeignKey(s => s.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ClinicId, s.AppointmentId, s.ChangedAtUtc });
    }
}
