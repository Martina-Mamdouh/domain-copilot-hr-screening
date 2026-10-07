using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Infrastructure.AI.Providers;
using DomainCopilot.Api.Infrastructure.Security;

namespace DomainCopilot.Api.Features.Screening.Services;

public class ScreeningPipelineService : IScreeningPipelineService
{
    private readonly ResilientLLMService _llmService;
    private readonly IPromptInjectionGuard _injectionGuard;
    private readonly ILogger<ScreeningPipelineService> _logger;

    public ScreeningPipelineService(
        ResilientLLMService llmService,
        IPromptInjectionGuard injectionGuard,
        ILogger<ScreeningPipelineService> logger)
    {
        _llmService = llmService;
        _injectionGuard = injectionGuard;
        _logger = logger;
    }

    public async Task<ScreeningResult> RunPipelineAsync(string rawCvText, string jobDescription, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting 3-Agent Screening Pipeline");
        var traces = new List<AgentExecutionTrace>();
        var sw = new Stopwatch();

        // Security Guard (Pre-Agent 2)
        _logger.LogInformation("Security Check: Running Injection Guard & Sanitization");
        sw.Start();
        bool injectionDetected = _injectionGuard.IsPotentiallyMalicious(rawCvText);
        string sanitizedCv = _injectionGuard.Sanitize(rawCvText);
        sw.Stop();

        traces.Add(new AgentExecutionTrace
        {
            AgentName = "PromptInjectionGuard",
            ProviderUsed = "LocalRegex",
            InputPayload = rawCvText.Length > 100 ? rawCvText.Substring(0, 100) + "..." : rawCvText,
            OutputPayload = $"Injection Detected: {injectionDetected}",
            ExecutionDurationMs = sw.ElapsedMilliseconds,
            Status = injectionDetected ? "Failed" : "Success"
        });

        if (injectionDetected)
        {
            _logger.LogWarning("Prompt injection detected! Pipeline halting evaluation.");
            return new ScreeningResult(
                new AgentOneResult("N/A", false),
                new AgentTwoResult(sanitizedCv, true),
                new AgentThreeResult(0, "REJECTED_SECURITY_RISK", "Malicious prompt injection detected in the CV."),
                traces
            );
        }

        // Agent 1: Extraction & Alignment
        _logger.LogInformation("Agent 1: Extracting skills and aligning with Job Description");
        sw.Restart();
        var agentOneSystemPrompt = "You are Agent 1. Extract the primary skills, experience, and education from the provided CV and output a summary. Then add a new line and write 'Meets Minimum: YES' or 'Meets Minimum: NO' comparing them to the Job Description.";
        var agentOneUserPrompt = $"Job Description: {jobDescription}\n\nCandidate CV: {sanitizedCv}";
        
        var (extractionResult, a1Provider) = await _llmService.GenerateTextWithFallbackAsync(agentOneSystemPrompt, agentOneUserPrompt, cancellationToken);
        sw.Stop();
        
        bool meetsMinimum = extractionResult.Contains("Meets Minimum: YES", StringComparison.OrdinalIgnoreCase);
        var agentOneResult = new AgentOneResult(extractionResult, meetsMinimum);

        traces.Add(new AgentExecutionTrace
        {
            AgentName = "Agent 1 (Extractor)",
            ProviderUsed = a1Provider,
            InputPayload = agentOneUserPrompt,
            OutputPayload = extractionResult,
            ExecutionDurationMs = sw.ElapsedMilliseconds,
            Status = "Success"
        });

        if (!meetsMinimum)
        {
            _logger.LogInformation("Candidate did not meet minimum requirements. Halting pipeline.");
            return new ScreeningResult(
                agentOneResult,
                new AgentTwoResult(sanitizedCv, false),
                new AgentThreeResult(0, "REJECTED_UNQUALIFIED", "Candidate lacks minimum required skills."),
                traces
            );
        }

        // Agent 2: Bias Defense (Demographic Redaction)
        _logger.LogInformation("Agent 2: Bias Defense (Anonymizing extracted profile)");
        sw.Restart();
        var agentTwoSystemPrompt = "You are Agent 2 (Bias Defense). Take the candidate's extracted profile and strictly strip/redact all personal demographic identifiers including Name, Gender, Age, Nationality, and specific Addresses to ensure fairness. Replace them with [REDACTED]. Return ONLY the anonymized profile.";
        var agentTwoUserPrompt = $"Extracted Profile:\n{extractionResult}";
        
        var (anonymizedProfile, a2Provider) = await _llmService.GenerateTextWithFallbackAsync(agentTwoSystemPrompt, agentTwoUserPrompt, cancellationToken);
        sw.Stop();

        var agentTwoResult = new AgentTwoResult(anonymizedProfile, false);

        traces.Add(new AgentExecutionTrace
        {
            AgentName = "Agent 2 (Bias Defense & Anonymizer)",
            ProviderUsed = a2Provider,
            InputPayload = agentTwoUserPrompt,
            OutputPayload = anonymizedProfile,
            ExecutionDurationMs = sw.ElapsedMilliseconds,
            Status = "Success"
        });

        // Agent 3: Evaluation & Scoring
        _logger.LogInformation("Agent 3: Final Evaluation and Scoring");
        sw.Restart();
        var agentThreeSystemPrompt = "You are Agent 3. Based on the anonymized candidate profile and the job description, provide a score from 0 to 100, a recommendation (HIRE, SHORTLIST, or REJECT), and a short 1-sentence reasoning (strengths/weaknesses). Format strictly as:\nSCORE: [number]\nRECOMMENDATION: [text]\nREASONING: [text]";
        var agentThreeUserPrompt = $"Job Description: {jobDescription}\n\nAnonymized Profile: {anonymizedProfile}";

        var (evaluationResult, a3Provider) = await _llmService.GenerateTextWithFallbackAsync(agentThreeSystemPrompt, agentThreeUserPrompt, cancellationToken);
        sw.Stop();
        
        // Parse Agent 3 Result
        int score = ParseScore(evaluationResult);
        string recommendation = ParseRecommendation(evaluationResult);
        string reasoning = ParseReasoning(evaluationResult);

        var agentThreeResult = new AgentThreeResult(score, recommendation, reasoning);

        traces.Add(new AgentExecutionTrace
        {
            AgentName = "Agent 3 (Evaluator)",
            ProviderUsed = a3Provider,
            InputPayload = agentThreeUserPrompt,
            OutputPayload = evaluationResult,
            ExecutionDurationMs = sw.ElapsedMilliseconds,
            Status = "Success"
        });

        _logger.LogInformation("Screening Pipeline Completed successfully.");

        return new ScreeningResult(
            agentOneResult,
            agentTwoResult,
            agentThreeResult,
            traces
        );
    }

    private int ParseScore(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("SCORE:", StringComparison.OrdinalIgnoreCase))
            {
                var numStr = line.Replace("SCORE:", "").Trim();
                if (int.TryParse(numStr, out int val)) return val;
            }
        }
        return 0; // Default
    }

    private string ParseRecommendation(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("RECOMMENDATION:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Replace("RECOMMENDATION:", "").Trim();
            }
        }
        return "UNKNOWN";
    }

    private string ParseReasoning(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("REASONING:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Replace("REASONING:", "").Trim();
            }
        }
        return "No reasoning provided by LLM.";
    }
}
