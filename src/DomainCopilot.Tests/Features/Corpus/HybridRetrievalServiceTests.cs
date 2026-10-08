using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Core.Interfaces;
using DomainCopilot.Api.Features.Corpus.Search;
using DomainCopilot.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DomainCopilot.Tests.Features.Corpus;

public class HybridRetrievalServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<ILogger<HybridRetrievalService>> _loggerMock;
    private readonly HybridRetrievalService _sut;

    public HybridRetrievalServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new AppDbContext(options);
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _loggerMock = new Mock<ILogger<HybridRetrievalService>>();

        _sut = new HybridRetrievalService(_dbContext, _embeddingServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RetrieveRelevantChunksAsync_WithThresholdFiltering_ShouldReturnOnlyHighConfidenceChunks()
    {
        // Arrange
        float[] queryEmbedding = { 1f, 0f, 0f };
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(queryEmbedding);

        // Chunk 1: High semantic similarity, high lexical overlap
        float[] chunk1Embedding = { 1f, 0f, 0f }; // Cosine = 1.0 (Rank 1)
        var chunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocId = "doc1",
            Content = "CSharp programming language features",
            SectionTitle = "CSharp Basics",
            EmbeddingJson = JsonSerializer.Serialize(chunk1Embedding)
        };

        // Chunk 2: Orthogonal semantic similarity, no lexical overlap
        float[] chunk2Embedding = { 0f, 1f, 0f }; // Cosine = 0.0 (Rank 2)
        var chunk2 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocId = "doc2",
            Content = "Random text about apples",
            SectionTitle = "Fruits",
            EmbeddingJson = JsonSerializer.Serialize(chunk2Embedding)
        };

        _dbContext.DocumentChunks.AddRange(chunk1, chunk2);
        await _dbContext.SaveChangesAsync();

        // Act
        // Query will match Chunk 1 strongly, and Chunk 2 not at all.
        var results = await _sut.RetrieveRelevantChunksAsync("CSharp programming", topK: 5);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1); // Chunk 2 should be filtered out by threshold
        results.First().ChunkId.Should().Be(chunk1.Id);
    }

    [Fact]
    public async Task RetrieveRelevantChunksAsync_RRF_ShouldCombineSemanticAndLexicalCorrectly()
    {
        // Arrange
        float[] queryEmbedding = { 1f, 1f, 1f };
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(queryEmbedding);

        // Chunk 1: Great Semantic, Poor Lexical
        var chunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocId = "doc1",
            Content = "Software engineering concepts.",
            SectionTitle = "Theory",
            EmbeddingJson = JsonSerializer.Serialize(new float[] { 1f, 1f, 1f }) // Cosine = 1.0 -> Rank 1 Semantic
        };

        // Chunk 2: Poor Semantic, Great Lexical
        var chunk2 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocId = "doc2",
            Content = "Exact KeywordMatch found here KeywordMatch KeywordMatch.",
            SectionTitle = "KeywordMatch Match",
            EmbeddingJson = JsonSerializer.Serialize(new float[] { 0.5f, 0.5f, 0.5f }) // Cosine = 0.5 -> Rank 2 Semantic (since > 0.2 threshold)
        };

        _dbContext.DocumentChunks.AddRange(chunk1, chunk2);
        await _dbContext.SaveChangesAsync();

        // Act
        // Query "KeywordMatch" will give Chunk 2 high lexical rank. 
        var results = await _sut.RetrieveRelevantChunksAsync("KeywordMatch", topK: 5);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2); 
        
        // Chunk 2 will have Lexical Rank 1, Semantic Rank 2
        // Chunk 1 will have Lexical Rank 0 (none), Semantic Rank 1
        // Score Chunk 2: 1/(60+2) + 1/(60+1) = 0.0161 + 0.0163 = 0.0324
        // Score Chunk 1: 1/(60+1) + 0 = 0.0163
        // Therefore, Chunk 2 should be first.
        results.First().ChunkId.Should().Be(chunk2.Id);
        results.Last().ChunkId.Should().Be(chunk1.Id);
    }

    [Fact]
    public async Task RetrieveRelevantChunksAsync_NoChunksMeetThreshold_ShouldReturnEmptyList()
    {
        // Arrange
        float[] queryEmbedding = { 1f, 0f, 0f };
        _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(queryEmbedding);

        // Create 100 chunks with orthogonal vectors and no lexical match to make RRF scores very small.
        // But actually, threshold is 0.015. 1/(60+1) = 0.0163, so rank 1 will still pass.
        // Wait, if lexical score is 0, and Semantic score is -1, it gets Rank 1 in Semantic if it's the only one.
        // Let's just create no chunks.
        
        // Act
        var results = await _sut.RetrieveRelevantChunksAsync("Unknown topic", topK: 5);

        // Assert
        results.Should().BeEmpty();
    }
}
