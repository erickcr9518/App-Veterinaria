using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(300);

        builder.HasIndex(p => p.Code).IsUnique();
    }
}
