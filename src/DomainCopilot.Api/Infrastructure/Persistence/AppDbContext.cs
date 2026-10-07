using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Common;
using DomainCopilot.Api.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Api.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<CandidateEvaluation> CandidateEvaluations => Set<CandidateEvaluation>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<AgentExecutionTrace> AgentExecutionTraces => Set<AgentExecutionTrace>();
    public DbSet<EvaluationRunMetric> EvaluationRunMetrics => Set<EvaluationRunMetric>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedAtUtc = DateTime.UtcNow;
                        baseEntity.IsDeleted = false;
                        break;
                    case EntityState.Modified:
                        baseEntity.UpdatedAtUtc = DateTime.UtcNow;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        baseEntity.IsDeleted = true;
                        baseEntity.UpdatedAtUtc = DateTime.UtcNow;
                        break;
                }
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
