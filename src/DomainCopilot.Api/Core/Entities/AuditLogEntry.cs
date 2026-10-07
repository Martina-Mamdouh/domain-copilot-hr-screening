using System;
using DomainCopilot.Api.Core.Common;

namespace DomainCopilot.Api.Core.Entities;

public class AuditLogEntry : BaseEntity
{
    public Guid CandidateEvaluationId { get; set; }
    public CandidateEvaluation Evaluation { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public Guid? PerformedByUserId { get; set; }
    public ApplicationUser? PerformedByUser { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
