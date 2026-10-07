using System.IO;
using DomainCopilot.Api.Features.Corpus.IngestDocument;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace DomainCopilot.Tests.Features.Corpus;

public class IngestDocumentCommandValidatorTests
{
    private readonly IngestDocumentCommandValidator _sut;

    public IngestDocumentCommandValidatorTests()
    {
        _sut = new IngestDocumentCommandValidator();
    }

    [Fact]
    public void Validate_WhenFileIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new IngestDocumentCommand(null!, "Resume", "ext-1");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "File" && e.ErrorMessage.Contains("File is required"));
    }

    [Fact]
    public void Validate_WhenFileIsEmpty_ShouldHaveError()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);
        
        var command = new IngestDocumentCommand(fileMock.Object, "Resume", "ext-1");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("File cannot be empty"));
    }

    [Fact]
    public void Validate_WhenFileExceeds10MB_ShouldHaveError()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns((10 * 1024 * 1024) + 1); // 1 byte over 10 MB
        
        var command = new IngestDocumentCommand(fileMock.Object, "Resume", "ext-1");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("File size must not exceed 10 MB"));
    }

    [Fact]
    public void Validate_WhenDocumentTypeIsEmpty_ShouldHaveError()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        
        var command = new IngestDocumentCommand(fileMock.Object, "", "ext-1");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentType");
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        
        var command = new IngestDocumentCommand(fileMock.Object, "Resume", "ext-1");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
