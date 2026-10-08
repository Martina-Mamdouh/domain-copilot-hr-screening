using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public class ResilientLLMService : IResilientLLMService
{
    private readonly OpenAILLMProvider _primaryProvider;
    private readonly OllamaLLMProvider _fallbackProvider;
    private readonly ILogger<ResilientLLMService> _logger;
    public ResilientLLMService(
        OpenAILLMProvider primaryProvider,
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
                
            var result = await _fallbackProvider.GenerateTextAsync(systemPrompt, userPrompt, cancellationToken);
            return (result, _fallbackProvider.ProviderName);
        }
    }
}
