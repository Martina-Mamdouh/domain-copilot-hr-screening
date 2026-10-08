using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public class ResilientEmbeddingService : IEmbeddingService
{
    private readonly OpenAIEmbeddingService _primaryService;
    private readonly OllamaEmbeddingService _fallbackService;
    private readonly ILogger<ResilientEmbeddingService> _logger;

    public ResilientEmbeddingService(
        OpenAIEmbeddingService primaryService,
        OllamaEmbeddingService fallbackService,
        ILogger<ResilientEmbeddingService> logger)
    {
        _primaryService = primaryService;
        _fallbackService = fallbackService;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.GenerateEmbeddingAsync(text, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary embedding service failed. Falling back to local Ollama.");
            try 
            {
                return await _fallbackService.GenerateEmbeddingAsync(text, cancellationToken);
            }
            catch (Exception ex2)
            {
                _logger.LogWarning(ex2, "Fallback embedding service also failed. Returning dummy embeddings.");
                return new float[768];
            }
        }
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.GenerateBatchEmbeddingsAsync(texts, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary batch embedding service failed. Falling back to local Ollama.");
            try 
            {
                return await _fallbackService.GenerateBatchEmbeddingsAsync(texts, cancellationToken);
            }
            catch (Exception ex2)
            {
                _logger.LogWarning(ex2, "Fallback batch embedding service also failed. Returning dummy embeddings.");
                var dummy = new List<float[]>();
                for (int i = 0; i < texts.Count; i++)
                {
                    dummy.Add(new float[768]);
                }
                return dummy;
            }
        }
    }
}
