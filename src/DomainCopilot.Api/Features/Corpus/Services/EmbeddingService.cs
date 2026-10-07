using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Embeddings;

namespace DomainCopilot.Api.Features.Corpus.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingGenerationService;

    public EmbeddingService(ITextEmbeddingGenerationService embeddingGenerationService)
    {
        _embeddingGenerationService = embeddingGenerationService;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingGenerationService.GenerateEmbeddingsAsync(new[] { text }, null, cancellationToken);
        return embeddings.First().ToArray();
    }

    public async Task<IList<float[]>> GenerateEmbeddingsAsync(IList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingGenerationService.GenerateEmbeddingsAsync(texts, null, cancellationToken);
        return embeddings.Select(e => e.ToArray()).ToList();
    }
}
