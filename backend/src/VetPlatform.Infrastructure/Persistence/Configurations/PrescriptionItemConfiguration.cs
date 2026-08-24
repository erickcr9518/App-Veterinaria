using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems");

        builder.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Concentration).HasMaxLength(100);
        builder.Property(i => i.Presentation).HasMaxLength(100);
        builder.Property(i => i.Quantity).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Route).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Frequency).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Duration).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Instructions).HasMaxLength(500);

        builder.HasOne(i => i.Prescription)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.ClinicId, i.PrescriptionId });
    }
}
