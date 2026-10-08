using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Core.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
