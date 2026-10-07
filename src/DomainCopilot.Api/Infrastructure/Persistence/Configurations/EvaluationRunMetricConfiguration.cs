using DomainCopilot.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomainCopilot.Api.Infrastructure.Persistence.Configurations;

public class EvaluationRunMetricConfiguration : IEntityTypeConfiguration<EvaluationRunMetric>
{
    public void Configure(EntityTypeBuilder<EvaluationRunMetric> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
