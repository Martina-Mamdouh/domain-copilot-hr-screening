using System;
using System.Collections.Generic;
using DomainCopilot.Api.Core.Common;
using DomainCopilot.Api.Core.Enums;

namespace DomainCopilot.Api.Core.Entities;

public class CandidateEvaluation : BaseEntity
{
    public string CandidateDocId { get; set; } = string.Empty;
    public string TargetJdId { get; set; } = string.Empty;
    public string CandidateAlias { get; set; } = string.Empty;
    public double WeightedScore { get; set; }
    public ScreeningDecision RecommendedDecision { get; set; }
    public ReviewStatus Status { get; set; }
    public string? ManagerOverrideReason { get; set; }
    public string? ManagerReviewerNotes { get; set; }
    public string CompetencyBreakdownJson { get; set; } = string.Empty;
    public Guid? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    
    public ICollection<AuditLogEntry> AuditLogs { get; set; } = new List<AuditLogEntry>();
    public ICollection<AgentExecutionTrace> AgentExecutionTraces { get; set; } = new List<AgentExecutionTrace>();
}
