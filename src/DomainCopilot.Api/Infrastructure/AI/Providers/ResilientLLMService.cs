using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public class ResilientLLMService
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

    public async Task<(string Text, string ProviderUsed)> GenerateTextWithFallbackAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var context = ResilienceContextPool.Shared.Get(cancellationToken);
        context.Properties.Set(new Polly.ResiliencePropertyKey<string>("systemPrompt"), systemPrompt);
        context.Properties.Set(new Polly.ResiliencePropertyKey<string>("userPrompt"), userPrompt);

        string providerUsed = _primaryProvider.ProviderName;
        
        var pipeline = new ResiliencePipelineBuilder<string>()
            .AddFallback(new Polly.Fallback.FallbackStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<Exception>(),
                FallbackAction = async args =>
                {
                    _logger.LogWarning(args.Context.Exception, "Primary LLM Provider (Gemini) failed. Falling back to Local Provider (Ollama).");
                    var sys = args.Context.Properties.GetValue(new Polly.ResiliencePropertyKey<string>("systemPrompt"), string.Empty);
                    var usr = args.Context.Properties.GetValue(new Polly.ResiliencePropertyKey<string>("userPrompt"), string.Empty);
                    
                    providerUsed = _fallbackProvider.ProviderName;
                    return await _fallbackProvider.GenerateTextAsync(sys, usr, args.Context.CancellationToken);
                }
            })
            .Build();

        try 
        {
            var result = await pipeline.ExecuteAsync(async ctx => 
            {
                return await _primaryProvider.GenerateTextAsync(systemPrompt, userPrompt, ctx.CancellationToken);
            }, context);
            
            return (result, providerUsed);
        }
        catch (Exception)
        {
            // If even the fallback fails
            throw;
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }
}
