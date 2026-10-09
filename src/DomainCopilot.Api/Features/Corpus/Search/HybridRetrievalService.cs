using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Common;
using DomainCopilot.Api.Core.Interfaces;
using DomainCopilot.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DomainCopilot.Api.Features.Corpus.Search;

public class HybridRetrievalService : IHybridRetrievalService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<HybridRetrievalService> _logger;

    public HybridRetrievalService(
        AppDbContext dbContext,
        IEmbeddingService embeddingService,
        ILogger<HybridRetrievalService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RetrievedChunkDto>> RetrieveRelevantChunksAsync(
        string query, 
        string? categoryFilter = null, 
        int topK = 5, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating embeddings for search query.");
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        
        var queryable = _dbContext.DocumentChunks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            if (categoryFilter.StartsWith("!")) 
            {
                var exclude = categoryFilter.Substring(1);
                queryable = queryable.Where(c => c.Category != exclude);
            } 
            else 
            {
                queryable = queryable.Where(c => c.Category == categoryFilter);
            }
        }
        
        var allChunks = await queryable.ToListAsync(cancellationToken);

        // 1. Semantic Search (Dense)
        var semanticResults = new List<(Guid ChunkId, float Score)>();
        foreach (var chunk in allChunks)
        {
            if (string.IsNullOrWhiteSpace(chunk.EmbeddingJson)) continue;
            var chunkEmbedding = JsonSerializer.Deserialize<float[]>(chunk.EmbeddingJson);
            if (chunkEmbedding == null) continue;

            float similarity = CosineSimilarity.Calculate(queryEmbedding, chunkEmbedding);
            if (similarity > 0.2f)
            {
                semanticResults.Add((chunk.Id, similarity));
            }
        }
        
        var rankedSemantic = semanticResults
            .OrderByDescending(x => x.Score)
            .Select((x, index) => new { x.ChunkId, Rank = index + 1, x.Score })
            .ToList();

        // 2. Lexical Search (Keyword matching)
        var stopWords = new HashSet<string> { "a", "an", "the", "and", "or", "but", "is", "are", "was", "were", "to", "in", "for", "of", "with", "on", "at", "by", "what", "how", "why", "when", "where", "it", "this", "that", "?" };
        
        var keywordKeywords = query.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(k => k.ToLowerInvariant())
                                   .Where(k => !stopWords.Contains(k))
                                   .ToList();

        var lexicalResults = new List<(Guid ChunkId, float Score)>();
        foreach (var chunk in allChunks)
        {
            float score = 0;
            var contentLower = chunk.Content.ToLowerInvariant();
            var titleLower = chunk.SectionTitle.ToLowerInvariant();

            foreach (var keyword in keywordKeywords)
            {
                if (titleLower.Contains(keyword)) score += 2.0f; // title match is stronger
                if (contentLower.Contains(keyword)) score += 1.0f;
            }
            if (score > 0)
            {
                lexicalResults.Add((chunk.Id, score));
            }
        }
        
        var rankedLexical = lexicalResults
            .OrderByDescending(x => x.Score)
            .Select((x, index) => new { x.ChunkId, Rank = index + 1, x.Score })
            .ToList();

        // 3. Reciprocal Rank Fusion (RRF)
        var fusionScores = new Dictionary<Guid, double>();
        const int k = 60; // Standard RRF constant

        foreach (var sr in rankedSemantic)
        {
            fusionScores[sr.ChunkId] = 1.0 / (k + sr.Rank);
        }

        foreach (var lr in rankedLexical)
        {
            if (!fusionScores.ContainsKey(lr.ChunkId))
                fusionScores[lr.ChunkId] = 0;
            fusionScores[lr.ChunkId] += 1.0 / (k + lr.Rank);
        }

        var topFusedChunks = fusionScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(topK)
            .ToList();

        // 4. Map to DTOs and apply Refusal Threshold
        const double MinimumThreshold = 0.015; // RRF scores are usually small, e.g., 1/61 = 0.016
        
        var results = new List<RetrievedChunkDto>();
        foreach (var fused in topFusedChunks)
        {
            if (fused.Value < MinimumThreshold) continue;

            var chunk = allChunks.First(c => c.Id == fused.Key);
            results.Add(new RetrievedChunkDto
            {
                ChunkId = chunk.Id,
                DocId = chunk.DocId,
                SectionTitle = chunk.SectionTitle,
                PageNumber = chunk.PageNumber,
                Content = chunk.Content,
                RelevanceScore = fused.Value
            });
        }

        return results;
    }
}
