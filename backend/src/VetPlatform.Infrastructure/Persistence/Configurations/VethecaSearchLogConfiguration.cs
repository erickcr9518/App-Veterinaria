using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities;

namespace VetPlatform.Infrastructure.Persistence.Configurations;

public class VethecaSearchLogConfiguration : IEntityTypeConfiguration<VethecaSearchLog>
{
    public void Configure(EntityTypeBuilder<VethecaSearchLog> builder)
    {
        builder.ToTable("VethecaSearchLogs");

        builder.Property(v => v.Question).HasMaxLength(1000).IsRequired();
        builder.Property(v => v.SearchQuery).HasMaxLength(500);
        builder.Property(v => v.ModelUsed).HasMaxLength(100);
        builder.Property(v => v.Title).HasMaxLength(200);
        builder.Property(v => v.ResultJson).IsRequired();
        builder.Property(v => v.FeedbackNote).HasMaxLength(1000);

        builder.HasIndex(v => new { v.ClinicId, v.CreatedByUserId, v.IsSaved });
    }
}
