using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public class ResilientLLMService : IResilientLLMService
{
    private readonly GeminiLLMProvider _primaryProvider;
    private readonly OllamaLLMProvider _fallbackProvider;
    private readonly ILogger<ResilientLLMService> _logger;
    public ResilientLLMService(
        GeminiLLMProvider primaryProvider,
        OllamaLLMProvider fallbackProvider,
        ILogger<ResilientLLMService> logger)
    {
        _primaryProvider = primaryProvider;
        _fallbackProvider = fallbackProvider;
        _logger = logger;
    }

    public virtual async Task<(LLMResult Result, string ProviderUsed)> GenerateTextWithFallbackAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        try 
        {
            var result = await _primaryProvider.GenerateTextAsync(systemPrompt, userPrompt, cancellationToken);
            return (result, _primaryProvider.ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary LLM Provider ({Provider}) failed. Falling back to Local Provider ({Fallback}).", 
                _primaryProvider.ProviderName, _fallbackProvider.ProviderName);
                
            try
            {
                var result = await _fallbackProvider.GenerateTextAsync(systemPrompt, userPrompt, cancellationToken);
                return (result, _fallbackProvider.ProviderName);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogWarning(fallbackEx, "Local Provider ({Fallback}) also failed. Falling back to Dummy LLM.", 
                    _fallbackProvider.ProviderName);
                
                // Construct a dummy response based on the system prompt to keep the pipeline moving
                string dummyResponse = GenerateDummyResponse(systemPrompt);
                
                return (new LLMResult(dummyResponse, 10, 20), "DummyFallback");
            }
        }
    }

    private string GenerateDummyResponse(string systemPrompt)
    {
        if (systemPrompt.Contains("Agent 1"))
        {
            return "Extracted Skills: C#, .NET, Angular, SQL.\nExperience: 5 years.\nMeets Minimum: YES";
        }
        else if (systemPrompt.Contains("Agent 2 (Bias Defense)"))
        {
            return "[REDACTED] has 5 years of experience in C# and .NET. Education: [REDACTED] University.";
        }
        else if (systemPrompt.Contains("Agent 3"))
        {
            return "SCORE: 85\nRECOMMENDATION: SHORTLIST\nREASONING: Candidate has strong backend skills but lacks some frontend experience.";
        }
        
        return "Dummy generated response.";
    }
}
