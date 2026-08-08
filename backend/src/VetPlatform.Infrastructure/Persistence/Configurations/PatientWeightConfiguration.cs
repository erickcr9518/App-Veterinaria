using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class PatientWeightConfiguration : IEntityTypeConfiguration<PatientWeight>
{
    public void Configure(EntityTypeBuilder<PatientWeight> builder)
    {
        builder.ToTable("PatientWeights");

        builder.Property(w => w.WeightKg).HasPrecision(6, 2);
        builder.Property(w => w.Notes).HasMaxLength(300);

        builder.HasOne(w => w.Patient)
            .WithMany(p => p.WeightHistory)
            .HasForeignKey(w => w.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.ClinicId, w.PatientId, w.RecordedAtUtc });
    }
}
