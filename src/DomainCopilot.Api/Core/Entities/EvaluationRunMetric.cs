using DomainCopilot.Api.Core.Common;

namespace DomainCopilot.Api.Core.Entities;

public class EvaluationRunMetric : BaseEntity
{
    public string RunName { get; set; } = string.Empty;
    public double AccuracyScore { get; set; }
    public int TotalEvaluated { get; set; }
    public int TotalMatched { get; set; }
    public double PromptInjectionDefenseRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public string? SummaryNotes { get; set; }
}
