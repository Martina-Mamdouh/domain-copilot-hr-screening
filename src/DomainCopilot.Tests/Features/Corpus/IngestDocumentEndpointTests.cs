using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DomainCopilot.Api;
using DomainCopilot.Api.Features.Corpus.IngestDocument;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Xunit;
using Microsoft.AspNetCore.Http;
using MediatR;
using Moq;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace DomainCopilot.Tests.Features.Corpus;

// Authentication Handler for Testing bypassing JWT
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) 
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "TestUser"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class IngestDocumentEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IngestDocumentEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_IngestDocument_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var mediatorMock = new Mock<ISender>();
        mediatorMock.Setup(m => m.Send(It.IsAny<IngestDocumentCommand>(), default))
            .ReturnsAsync(new IngestDocumentResponse("Document ingested successfully.", 5));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("UseInMemoryDatabase", "true")
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options => 
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                
                services.AddScoped<ISender>(_ => mediatorMock.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        
        content.Add(fileContent, "file", "test.pdf");
        content.Add(new StringContent("Resume"), "documentType");
        content.Add(new StringContent("ref-123"), "externalReferenceId");

        // Act
        var response = await client.PostAsync("/api/corpus/ingest", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        resultString.Should().Contain("Document ingested successfully");
        resultString.Should().Contain("5");
    }

    [Fact]
    public async Task Post_IngestDocument_WithException_ReturnsBadRequest()
    {
        // Arrange
        var mediatorMock = new Mock<ISender>();
        mediatorMock.Setup(m => m.Send(It.IsAny<IngestDocumentCommand>(), default))
            .ThrowsAsync(new ArgumentException("Unsupported file extension. Only .pdf, .md, and .txt are supported."));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("UseInMemoryDatabase", "true")
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options => 
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                
                services.AddScoped<ISender>(_ => mediatorMock.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        
        content.Add(fileContent, "file", "test.docx");
        content.Add(new StringContent("Resume"), "documentType");

        // Act
        var response = await client.PostAsync("/api/corpus/ingest", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var resultString = await response.Content.ReadAsStringAsync();
        resultString.Should().Contain("Unsupported file extension");
    }
}
