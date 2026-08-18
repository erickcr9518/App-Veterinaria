using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class SoapNoteConfiguration : IEntityTypeConfiguration<SoapNote>
{
    public void Configure(EntityTypeBuilder<SoapNote> builder)
    {
        builder.ToTable("SoapNotes");

        builder.Property(s => s.Subjective).HasMaxLength(2000);
        builder.Property(s => s.Objective).HasMaxLength(2000);
        builder.Property(s => s.Assessment).HasMaxLength(2000);
        builder.Property(s => s.Plan).HasMaxLength(2000);

        builder.HasOne(s => s.Consultation)
            .WithOne(c => c.SoapNote)
            .HasForeignKey<SoapNote>(s => s.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.ConsultationId).IsUnique();
    }
}
