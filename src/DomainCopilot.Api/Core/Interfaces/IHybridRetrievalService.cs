using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Core.Interfaces;

public interface IHybridRetrievalService
{
    Task<IReadOnlyList<RetrievedChunkDto>> RetrieveRelevantChunksAsync(
        string query, 
        string? categoryFilter = null, 
        int topK = 5, 
        CancellationToken cancellationToken = default);
}
