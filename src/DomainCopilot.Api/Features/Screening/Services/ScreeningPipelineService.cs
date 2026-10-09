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
    private readonly IResilientLLMService _llmService;
    private readonly IPromptInjectionGuard _injectionGuard;
    private readonly DomainCopilot.Api.Core.Interfaces.IGroundedGenerationService _groundedGenerationService;
    private readonly ILogger<ScreeningPipelineService> _logger;

    public ScreeningPipelineService(
        IResilientLLMService llmService,
        IPromptInjectionGuard injectionGuard,
        DomainCopilot.Api.Core.Interfaces.IGroundedGenerationService groundedGenerationService,
        ILogger<ScreeningPipelineService> logger)
    {
        _llmService = llmService;
        _injectionGuard = injectionGuard;
        _groundedGenerationService = groundedGenerationService;
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
                new AgentThreeResult(0, "REJECTED_SECURITY_RISK", "Malicious prompt injection detected in the CV.", "N/A"),
                traces
            );
        }

        // Agent 1: Extraction & Alignment
        _logger.LogInformation("Agent 1: Extracting skills and aligning with Job Description");
        sw.Restart();
        var agentOneSystemPrompt = "You are Agent 1. Extract the primary skills, experience, and education from the provided CV and output a summary. Then add a new line and write 'Meets Minimum: YES' or 'Meets Minimum: NO' comparing them to the Job Description.";
        var agentOneUserPrompt = $"Job Description: {jobDescription}\n\nCandidate CV: {sanitizedCv}";
        
        var llmOutputOne = await _llmService.GenerateTextWithFallbackAsync(agentOneSystemPrompt, agentOneUserPrompt, cancellationToken);
        sw.Stop();
        
        // If the LLM didn't explicitly say "Meets Minimum: NO", we assume it passes to allow the pipeline to continue.
        // Small local models (like llama3.2) often forget to include the exact string.
        bool explicitlyRejected = llmOutputOne.Result.Text.Contains("Meets Minimum: NO", StringComparison.OrdinalIgnoreCase) || 
                                  llmOutputOne.Result.Text.Contains("does not meet", StringComparison.OrdinalIgnoreCase);
        
        bool meetsMinimum = !explicitlyRejected;
        
        var agentOneResult = new AgentOneResult(llmOutputOne.Result.Text, meetsMinimum);

        traces.Add(new AgentExecutionTrace
        {
            AgentName = "Agent 1 (Extractor)",
            ProviderUsed = llmOutputOne.ProviderUsed,
            InputPayload = agentOneUserPrompt,
            OutputPayload = llmOutputOne.Result.Text,
            ExecutionDurationMs = sw.ElapsedMilliseconds,
            PromptTokens = llmOutputOne.Result.PromptTokens,
            CompletionTokens = llmOutputOne.Result.CompletionTokens,
            Status = "Success"
        });

        if (!meetsMinimum)
        {
            _logger.LogInformation("Candidate did not meet minimum requirements. Halting pipeline.");
            return new ScreeningResult(
                agentOneResult,
                new AgentTwoResult(sanitizedCv, false),
                new AgentThreeResult(0, "REJECTED_UNQUALIFIED", "Candidate lacks minimum required skills.", "N/A"),
                traces
            );
        }

        // Agent 2: Bias Defense (Demographic Redaction)
        _logger.LogInformation("Agent 2: Bias Defense (Anonymizing extracted profile)");
        sw.Restart();
        var agentTwoSystemPrompt = "You are Agent 2 (Bias Defense). Take the candidate's extracted profile and strictly strip/redact all personal demographic identifiers including Name, Gender, Age, Nationality, and specific Addresses to ensure fairness. Replace them with [REDACTED]. Return ONLY the anonymized profile.";
        var agentTwoUserPrompt = $"Extracted Profile:\n{llmOutputOne.Result.Text}";
        
        var llmOutputTwo = await _llmService.GenerateTextWithFallbackAsync(agentTwoSystemPrompt, agentTwoUserPrompt, cancellationToken);
        sw.Stop();

        var agentTwoResult = new AgentTwoResult(llmOutputTwo.Result.Text, false);

        traces.Add(new AgentExecutionTrace
        {
            AgentName = "Agent 2 (Bias Defense & Anonymizer)",
            ProviderUsed = llmOutputTwo.ProviderUsed,
            InputPayload = agentTwoUserPrompt,
            OutputPayload = llmOutputTwo.Result.Text,
            ExecutionDurationMs = sw.ElapsedMilliseconds,
            PromptTokens = llmOutputTwo.Result.PromptTokens,
            CompletionTokens = llmOutputTwo.Result.CompletionTokens,
            Status = "Success"
        });

        // Agent 2.5: RAG Evidence Extraction
        _logger.LogInformation("Agent 2.5: Extracting Evaluation Evidence from Corpus");
        sw.Restart();
        var evidenceQuery = $"What are the internal evaluation guidelines, required certifications, and standards for the following job description: {jobDescription}";
        var evidence = await _groundedGenerationService.GenerateAnswerAsync(evidenceQuery, null, cancellationToken);
        sw.Stop();

        traces.Add(new AgentExecutionTrace
        {
            AgentName = "Agent 2.5 (RAG Evidence Extractor)",
            ProviderUsed = "DomainCopilot.Corpus",
            InputPayload = evidenceQuery,
            OutputPayload = evidence.Answer,
            ExecutionDurationMs = sw.ElapsedMilliseconds,
            Status = evidence.IsGrounded ? "Success" : "No Evidence Found"
        });

        // Agent 3: Evaluation & Scoring
        _logger.LogInformation("Agent 3: Final Evaluation and Scoring");
        sw.Restart();
        var agentThreeSystemPrompt = "You are Agent 3. Based on the anonymized candidate profile, the job description, and the internal evaluation evidence provided, provide a score from 0 to 100, a recommendation (HIRE, SHORTLIST, or REJECT), a short 1-sentence reasoning (strengths/weaknesses), and 3 Interview Probes (questions to ask the candidate to test their gaps). Format strictly as:\nSCORE: [number]\nRECOMMENDATION: [text]\nREASONING: [text]\nPROBES: [text]";
        var agentThreeUserPrompt = $"Job Description: {jobDescription}\n\nInternal Evaluation Evidence:\n{evidence.Answer}\n\nAnonymized Profile: {llmOutputTwo.Result.Text}";

        var llmOutputThree = await _llmService.GenerateTextWithFallbackAsync(agentThreeSystemPrompt, agentThreeUserPrompt, cancellationToken);
        sw.Stop();
        
        // Parse Agent 3 Result
        int score = ParseScore(llmOutputThree.Result.Text);
        string recommendation = ParseRecommendation(llmOutputThree.Result.Text);
        string reasoning = ParseReasoning(llmOutputThree.Result.Text);
        string probes = ParseProbes(llmOutputThree.Result.Text);

        var agentThreeResult = new AgentThreeResult(score, recommendation, reasoning, probes, evidence.Answer, evidence.Sources);

        traces.Add(new AgentExecutionTrace
        {
            AgentName = "Agent 3 (Evaluator)",
            ProviderUsed = llmOutputThree.ProviderUsed,
            InputPayload = agentThreeUserPrompt,
            OutputPayload = llmOutputThree.Result.Text,
            ExecutionDurationMs = sw.ElapsedMilliseconds,
            PromptTokens = llmOutputThree.Result.PromptTokens,
            CompletionTokens = llmOutputThree.Result.CompletionTokens,
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
        var match = System.Text.RegularExpressions.Regex.Match(text, @"SCORE:\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int val)) return val;
        return 0; // Default
    }

    private string ParseRecommendation(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"RECOMMENDATION:\s*([a-zA-Z]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value.Trim().ToUpper();
        return "UNKNOWN";
    }

    private string ParseReasoning(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"REASONING:\s*(.+?)(?=\nPROBES:|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value.Trim();
        return "No reasoning provided by LLM.";
    }

    private string ParseProbes(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"PROBES:\s*(.+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value.Trim();
        return "No interview probes provided.";
    }
}
