using DomainCopilot.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Api.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(a => a.Action).HasMaxLength(100);

        builder.HasOne(e => e.Evaluation)
            .WithMany(e => e.AuditLogs)
            .HasForeignKey(e => e.CandidateEvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PerformedByUser)
            .WithMany()
            .HasForeignKey(e => e.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
