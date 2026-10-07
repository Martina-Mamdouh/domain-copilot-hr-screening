using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Features.Corpus.Services;
using DomainCopilot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Embeddings;

namespace DomainCopilot.Api.Features.Corpus.Search;

public class SearchHandler : IRequestHandler<SearchQuery, SearchResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;

    public SearchHandler(AppDbContext dbContext, IEmbeddingService embeddingService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
    }

    public async Task<SearchResponse> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        // Generate embedding for the search query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
        
        // Fetch all chunks that have an embedding.
        // NOTE: In a production scenario, you would use a Vector Database (like Qdrant, Pinecone, or PGVector)
        // Since we are using standard SQL Server without Vector extension, we fetch into memory.
        var chunks = await _dbContext.DocumentChunks
            .Where(c => c.EmbeddingJson != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var results = chunks.Select(c =>
        {
            var chunkEmbedding = System.Text.Json.JsonSerializer.Deserialize<float[]>(c.EmbeddingJson!);
            var similarity = CosineSimilarity(queryEmbedding, chunkEmbedding!);
            
            return new SearchResult(c.Content, c.DocId, c.Category, c.SectionTitle, similarity);
        })
        .Where(r => r.SimilarityScore >= request.SimilarityThreshold)
        .OrderByDescending(r => r.SimilarityScore)
        .Take(request.TopK)
        .ToList();

        return new SearchResponse(request.Query, results);
    }

    private static float CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0;

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (float)(Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
    }
}
