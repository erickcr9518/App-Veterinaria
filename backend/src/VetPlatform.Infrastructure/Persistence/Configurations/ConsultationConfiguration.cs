using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;
using VetPlatform.Infrastructure.Identity;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.ToTable("Consultations");

        builder.Property(c => c.ReasonForVisit).HasMaxLength(500).IsRequired();
        builder.Property(c => c.HistoryOfPresentIllness).HasMaxLength(2000);
        builder.Property(c => c.PhysicalExamFindings).HasMaxLength(2000);
        builder.Property(c => c.TemperatureCelsius).HasPrecision(4, 1);
        builder.Property(c => c.WeightKg).HasPrecision(6, 2);
        builder.Property(c => c.DiagnosticPlan).HasMaxLength(2000);
        builder.Property(c => c.Treatment).HasMaxLength(2000);
        builder.Property(c => c.Recommendations).HasMaxLength(2000);
        builder.Property(c => c.Status).HasMaxLength(20).IsRequired();

        builder.HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.VeterinarianUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.FinalizedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.ClinicId, c.PatientId, c.ConsultationDateUtc });
        builder.HasIndex(c => new { c.ClinicId, c.Status });
    }
}
