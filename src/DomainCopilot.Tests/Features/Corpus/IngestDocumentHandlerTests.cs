using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Features.Corpus.IngestDocument;
using DomainCopilot.Api.Features.Corpus.Services;
using DomainCopilot.Api.Core.Interfaces;
using DomainCopilot.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DomainCopilot.Tests.Features.Corpus;

public class IngestDocumentHandlerTests
{
    private readonly Mock<IDocumentParserService> _parserServiceMock;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly AppDbContext _dbContext;
    private readonly IngestDocumentHandler _sut;

    public IngestDocumentHandlerTests()
    {
        _parserServiceMock = new Mock<IDocumentParserService>();
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _dbContext = new AppDbContext(options);
        
        _sut = new IngestDocumentHandler(_parserServiceMock.Object, _dbContext, _embeddingServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidFile_ShouldReturnResponseAndSaveChunks()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("document.pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        var command = new IngestDocumentCommand(fileMock.Object, "Resume", "ext-123");

        var expectedChunks = new List<DocumentChunk>
        {
            new() { DocId = "ext-123", Content = "chunk 1", WordCount = 2 },
            new() { DocId = "ext-123", Content = "chunk 2", WordCount = 2 }
        };

        _parserServiceMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), "document.pdf", "Resume", "ext-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChunks);

        var expectedEmbeddings = new List<float[]> { new float[] { 0.1f }, new float[] { 0.2f } };
        _embeddingServiceMock.Setup(e => e.GenerateBatchEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbeddings);

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TotalChunks.Should().Be(2);
        response.Message.Should().Be("Document ingested successfully.");

        var savedChunks = await _dbContext.DocumentChunks.ToListAsync();
        savedChunks.Should().HaveCount(2);
        savedChunks[0].DocId.Should().Be("ext-123");
    }

    [Fact]
    public async Task Handle_WithUnsupportedExtension_ShouldThrowArgumentException()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("document.docx");
        
        var command = new IngestDocumentCommand(fileMock.Object, "Resume", null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported file extension*");
    }

    [Fact]
    public async Task Handle_WithNoExtractedText_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("empty.pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        var command = new IngestDocumentCommand(fileMock.Object, "Resume", null);

        _parserServiceMock.Setup(p => p.ParseAsync(It.IsAny<Stream>(), "empty.pdf", "Resume", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentChunk>());

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No text could be extracted*");
    }
}
