using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class VethecaLibraryChunkConfiguration : IEntityTypeConfiguration<VethecaLibraryChunk>
{
    public void Configure(EntityTypeBuilder<VethecaLibraryChunk> builder)
    {
        builder.ToTable("VethecaLibraryChunks");

        builder.Property(c => c.Text).IsRequired();

        builder.HasIndex(c => c.DocumentId);
        builder.HasIndex(c => c.ClinicId);
    }
}
