using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("Owners");

        builder.Property(o => o.FullName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.IdentificationNumber).HasMaxLength(50);
        builder.Property(o => o.Phone).HasMaxLength(50).IsRequired();
        builder.Property(o => o.Email).HasMaxLength(200);
        builder.Property(o => o.Address).HasMaxLength(300);
        builder.Property(o => o.AlternateContact).HasMaxLength(200);
        builder.Property(o => o.ConsentNotes).HasMaxLength(1000);

        builder.HasIndex(o => new { o.ClinicId, o.FullName });
        builder.HasIndex(o => new { o.ClinicId, o.IdentificationNumber });
        builder.HasIndex(o => new { o.ClinicId, o.Phone });
    }
}
