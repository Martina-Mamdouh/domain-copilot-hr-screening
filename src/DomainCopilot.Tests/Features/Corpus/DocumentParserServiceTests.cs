using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Features.Corpus.Services;
using FluentAssertions;
using Xunit;

namespace DomainCopilot.Tests.Features.Corpus;

public class DocumentParserServiceTests
{
    private readonly DocumentParserService _sut;

    public DocumentParserServiceTests()
    {
        _sut = new DocumentParserService();
    }

    [Fact]
    public async Task ParseAsync_WithMarkdown_ShouldExtractHeadersAndChunkBody()
    {
        // Arrange
        var markdownContent = @"
# Introduction
This is the intro text.
## Details
Here are some details.
### Summary
The final summary.";
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdownContent));
        
        // Act
        var chunks = await _sut.ParseAsync(stream, "test.md", "Resume", "doc-123", CancellationToken.None);

        // Assert
        chunks.Should().NotBeNull();
        chunks.Should().HaveCount(3);
        
        chunks[0].SectionTitle.Should().Be("Introduction");
        chunks[0].Content.Should().Be("This is the intro text.");
        chunks[0].Category.Should().Be("Resume");
        chunks[0].DocId.Should().Be("doc-123");

        chunks[1].SectionTitle.Should().Be("Details");
        chunks[1].Content.Should().Be("Here are some details.");

        chunks[2].SectionTitle.Should().Be("Summary");
        chunks[2].Content.Should().Be("The final summary.");
    }

    [Fact]
    public async Task ParseAsync_WithLargeText_ShouldChunkWithOverlap()
    {
        // Arrange
        // Create 400 words
        var words = Enumerable.Range(1, 400).Select(i => $"word{i}");
        var text = string.Join(" ", words);
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        // Act
        var chunks = await _sut.ParseAsync(stream, "test.txt", "Report", "doc-456", CancellationToken.None);

        // Assert
        chunks.Should().HaveCount(2);
        
        // First chunk should have 350 words
        chunks[0].WordCount.Should().Be(350);
        chunks[0].ChunkIndex.Should().Be(0);
        
        // Second chunk should start with overlap (overlap is 50 words)
        // Words left: 400 - 350 = 50. Plus 50 overlap = 100 words in second chunk.
        chunks[1].WordCount.Should().Be(100);
        chunks[1].ChunkIndex.Should().Be(1);
    }

    [Fact]
    public async Task ParseAsync_WithPromptInjectionTokens_ShouldSanitizeContent()
    {
        // Arrange
        var text = "Hello <|im_start|> dangerous <|im_end|> world.";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        // Act
        var chunks = await _sut.ParseAsync(stream, "test.txt", "General", "doc-789", CancellationToken.None);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().NotContain("<|im_start|>");
        chunks[0].Content.Should().NotContain("<|im_end|>");
        chunks[0].Content.Should().Be("Hello  dangerous  world.");
    }

    [Fact]
    public async Task ParseAsync_WithUnsupportedExtension_ShouldThrowNotSupportedException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        Func<Task> act = async () => await _sut.ParseAsync(stream, "test.jpg", "Image", "doc-999", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*not supported*");
    }

    [Fact]
    public async Task ParseAsync_WithEmptyFile_ShouldReturnEmptyChunks()
    {
        // Arrange
        var text = "   \n  \t  ";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        // Act
        var chunks = await _sut.ParseAsync(stream, "test.md", "General", "doc-empty", CancellationToken.None);

        // Assert
        chunks.Should().BeEmpty();
    }
}
