using DomainCopilot.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Api.Infrastructure.Persistence.Configurations;

public class CandidateEvaluationConfiguration : IEntityTypeConfiguration<CandidateEvaluation>
{
    public void Configure(EntityTypeBuilder<CandidateEvaluation> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(c => c.CandidateDocId).HasMaxLength(100);

        builder.HasIndex(c => c.CandidateDocId);

        builder.HasOne(e => e.ReviewedByUser)
            .WithMany()
            .HasForeignKey(e => e.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
