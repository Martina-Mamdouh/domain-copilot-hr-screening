using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Features.Corpus.Services;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IList<float[]>> GenerateEmbeddingsAsync(IList<string> texts, CancellationToken cancellationToken = default);
}
