using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class ConsultationAmendmentConfiguration : IEntityTypeConfiguration<ConsultationAmendment>
{
    public void Configure(EntityTypeBuilder<ConsultationAmendment> builder)
    {
        builder.ToTable("ConsultationAmendments");

        builder.Property(a => a.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.PreviousValuesJson).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasOne(a => a.Consultation)
            .WithMany(c => c.Amendments)
            .HasForeignKey(a => a.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.ClinicId, a.ConsultationId, a.CreatedAtUtc });
    }
}
