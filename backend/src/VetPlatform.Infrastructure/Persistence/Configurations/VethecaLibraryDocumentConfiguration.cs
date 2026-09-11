using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class VethecaLibraryDocumentConfiguration : IEntityTypeConfiguration<VethecaLibraryDocument>
{
    public void Configure(EntityTypeBuilder<VethecaLibraryDocument> builder)
    {
        builder.ToTable("VethecaLibraryDocuments");

        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(260).IsRequired();

        builder.HasMany(d => d.Chunks)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.ClinicId);
    }
}
