using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;
using VetPlatform.Infrastructure.Identity;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");

        builder.Property(p => p.WeightKgAtPrescription).HasPrecision(6, 2);
        builder.Property(p => p.GeneralInstructions).HasMaxLength(2000);
        builder.Property(p => p.Warnings).HasMaxLength(1000);
        builder.Property(p => p.Status).HasMaxLength(20).IsRequired();

        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Consultation)
            .WithMany()
            .HasForeignKey(p => p.ConsultationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.VeterinarianUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.FinalizedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ClinicId, p.PatientId, p.IssuedAtUtc });
        builder.HasIndex(p => new { p.ClinicId, p.ConsultationId });
    }
}
