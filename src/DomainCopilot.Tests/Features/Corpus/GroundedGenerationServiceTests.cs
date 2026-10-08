using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Interfaces;
using DomainCopilot.Api.Features.Corpus.Generation;
using DomainCopilot.Api.Infrastructure.AI.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DomainCopilot.Tests.Features.Corpus;

public class GroundedGenerationServiceTests
{
    private readonly Mock<IHybridRetrievalService> _retrievalServiceMock;
    private readonly Mock<IResilientLLMService> _llmServiceMock;
    private readonly Mock<ILogger<GroundedGenerationService>> _loggerMock;
    private readonly GroundedGenerationService _sut;

    public GroundedGenerationServiceTests()
    {
        _retrievalServiceMock = new Mock<IHybridRetrievalService>();
        
        // Mock IResilientLLMService directly
        _llmServiceMock = new Mock<IResilientLLMService>();

        _loggerMock = new Mock<ILogger<GroundedGenerationService>>();
        
        _sut = new GroundedGenerationService(_retrievalServiceMock.Object, _llmServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithRelevantChunks_ReturnsGroundedAnswer()
    {
        // Arrange
        var chunks = new List<RetrievedChunkDto>
        {
            new RetrievedChunkDto
            {
                ChunkId = Guid.NewGuid(),
                DocId = "doc123",
                SectionTitle = "Overview",
                Content = "This is the actual answer."
            }
        };

        _retrievalServiceMock.Setup(r => r.RetrieveRelevantChunksAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        var expectedLlmResult = (new LLMResult("Here is the answer based on the context. [Source: doc123 | Section: Overview]", 100, 50), "Gemini");

        _llmServiceMock.Setup(l => l.GenerateTextWithFallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLlmResult);

        // Act
        var result = await _sut.GenerateAnswerAsync("What is the answer?");

        // Assert
        result.Should().NotBeNull();
        result.IsGrounded.Should().BeTrue();
        result.Answer.Should().Be(expectedLlmResult.Item1.Text);
        result.Sources.Should().HaveCount(1);
        result.Sources.First().DocId.Should().Be("doc123");
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithNoRelevantChunks_ReturnsRefusal()
    {
        // Arrange
        var chunks = new List<RetrievedChunkDto>();

        _retrievalServiceMock.Setup(r => r.RetrieveRelevantChunksAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        // Act
        var result = await _sut.GenerateAnswerAsync("What is the answer?");

        // Assert
        result.Should().NotBeNull();
        result.IsGrounded.Should().BeFalse();
        result.Answer.Should().Be("The requested information is not available in the ingested documents.");
        result.Sources.Should().BeEmpty();
        
        // Verify LLM was not called
        _llmServiceMock.Verify(l => l.GenerateTextWithFallbackAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
