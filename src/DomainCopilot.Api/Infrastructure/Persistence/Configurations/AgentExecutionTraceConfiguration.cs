using DomainCopilot.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Api.Infrastructure.Persistence.Configurations;

public class AgentExecutionTraceConfiguration : IEntityTypeConfiguration<AgentExecutionTrace>
{
    public void Configure(EntityTypeBuilder<AgentExecutionTrace> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(a => a.AgentName).HasMaxLength(100);

        builder.HasOne(e => e.Evaluation)
            .WithMany(e => e.AgentExecutionTraces)
            .HasForeignKey(e => e.CandidateEvaluationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
