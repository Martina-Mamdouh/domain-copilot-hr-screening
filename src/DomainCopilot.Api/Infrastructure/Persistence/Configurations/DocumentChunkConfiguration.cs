using DomainCopilot.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Api.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(d => d.DocId).HasMaxLength(100);
        builder.Property(d => d.Category).HasMaxLength(50);

        builder.HasIndex(d => d.DocId);
        builder.HasIndex(d => d.Category);
    }
}
