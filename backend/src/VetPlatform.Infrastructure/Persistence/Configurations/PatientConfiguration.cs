using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Species).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Breed).HasMaxLength(120);
        builder.Property(p => p.EstimatedAge).HasMaxLength(80);
        builder.Property(p => p.Sex).HasMaxLength(30).IsRequired();
        builder.Property(p => p.ReproductiveStatus).HasMaxLength(80);
        builder.Property(p => p.Color).HasMaxLength(80);
        builder.Property(p => p.CurrentWeightKg).HasPrecision(6, 2);
        builder.Property(p => p.MicrochipNumber).HasMaxLength(80);
        builder.Property(p => p.PhotoUrl).HasMaxLength(500);
        builder.Property(p => p.Allergies).HasMaxLength(1000);
        builder.Property(p => p.ChronicDiseases).HasMaxLength(1000);
        builder.Property(p => p.CurrentMedications).HasMaxLength(1000);
        builder.Property(p => p.VaccinationStatus).HasMaxLength(500);
        builder.Property(p => p.DewormingStatus).HasMaxLength(500);
        builder.Property(p => p.Status).HasMaxLength(40).IsRequired();

        builder.HasOne(p => p.Owner)
            .WithMany(o => o.Patients)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ClinicId, p.Name });
        builder.HasIndex(p => new { p.ClinicId, p.OwnerId });
        builder.HasIndex(p => new { p.ClinicId, p.MicrochipNumber });
    }
}
