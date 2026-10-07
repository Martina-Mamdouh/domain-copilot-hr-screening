using System;
using DomainCopilot.Api.Core.Common;

namespace DomainCopilot.Api.Core.Entities;

public class AgentExecutionTrace : BaseEntity
{
    public Guid CandidateEvaluationId { get; set; }
    public CandidateEvaluation Evaluation { get; set; } = null!;
    public string AgentName { get; set; } = string.Empty;
    public string InputPayload { get; set; } = string.Empty;
    public string OutputPayload { get; set; } = string.Empty;
    public long ExecutionDurationMs { get; set; }
    public string ProviderUsed { get; set; } = string.Empty;
    public string Status { get; set; } = "Success";
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
}
