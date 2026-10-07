using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        // Agent 2: Bias Defense & Injection Guard (Runs first for security)
        _logger.LogInformation("Agent 2: Running Injection Guard & Sanitization");
        bool injectionDetected = _injectionGuard.IsPotentiallyMalicious(rawCvText);
        string sanitizedCv = _injectionGuard.Sanitize(rawCvText);
        
        var agentTwoResult = new AgentTwoResult(sanitizedCv, injectionDetected);

        if (injectionDetected)
        {
            _logger.LogWarning("Prompt injection detected! Pipeline halting evaluation.");
            return new ScreeningResult(
                new AgentOneResult("N/A", false),
                agentTwoResult,
                new AgentThreeResult(0, "REJECTED_SECURITY_RISK", "Malicious prompt injection detected in the CV."),
                "SecurityGuard"
            );
        }

        // Agent 1: Extraction & Alignment
        _logger.LogInformation("Agent 1: Extracting skills and aligning with Job Description");
        var agentOneSystemPrompt = "You are Agent 1. Extract the primary skills from the provided CV and output a comma-separated list. Then add a new line and write 'Meets Minimum: YES' or 'Meets Minimum: NO' comparing them to the Job Description.";
        var agentOneUserPrompt = $"Job Description: {jobDescription}\n\nCandidate CV: {sanitizedCv}";
        
        var (extractionResult, providerUsed) = await _llmService.GenerateTextWithFallbackAsync(agentOneSystemPrompt, agentOneUserPrompt, cancellationToken);
        
        bool meetsMinimum = extractionResult.Contains("Meets Minimum: YES", StringComparison.OrdinalIgnoreCase);
        var agentOneResult = new AgentOneResult(extractionResult, meetsMinimum);

        if (!meetsMinimum)
        {
            _logger.LogInformation("Candidate did not meet minimum requirements. Halting pipeline.");
            return new ScreeningResult(
                agentOneResult,
                agentTwoResult,
                new AgentThreeResult(0, "REJECTED_UNQUALIFIED", "Candidate lacks minimum required skills."),
                providerUsed
            );
        }

        // Agent 3: Evaluation & Scoring
        _logger.LogInformation("Agent 3: Final Evaluation and Scoring");
        var agentThreeSystemPrompt = "You are Agent 3. Based on the extracted skills and the job description, provide a score from 0 to 100, a recommendation (HIRE, SHORTLIST, or REJECT), and a short 1-sentence reasoning. Format strictly as:\nSCORE: [number]\nRECOMMENDATION: [text]\nREASONING: [text]";
        var agentThreeUserPrompt = $"Job Description: {jobDescription}\n\nExtracted Skills: {agentOneResult.ExtractedSkills}";

        var (evaluationResult, evalProviderUsed) = await _llmService.GenerateTextWithFallbackAsync(agentThreeSystemPrompt, agentThreeUserPrompt, cancellationToken);
        
        // Parse Agent 3 Result
        int score = ParseScore(evaluationResult);
        string recommendation = ParseRecommendation(evaluationResult);
        string reasoning = ParseReasoning(evaluationResult);

        var agentThreeResult = new AgentThreeResult(score, recommendation, reasoning);

        _logger.LogInformation("Screening Pipeline Completed successfully.");

        return new ScreeningResult(
            agentOneResult,
            agentTwoResult,
            agentThreeResult,
            evalProviderUsed
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
