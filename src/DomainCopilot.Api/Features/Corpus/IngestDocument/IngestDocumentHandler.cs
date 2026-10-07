using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Features.Corpus.Services;
using DomainCopilot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace DomainCopilot.Api.Features.Corpus.IngestDocument;

public class IngestDocumentHandler : IRequestHandler<IngestDocumentCommand, IngestDocumentResponse>
{
    private readonly IDocumentParserService _parserService;
    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;

    public IngestDocumentHandler(IDocumentParserService parserService, AppDbContext dbContext, IEmbeddingService embeddingService)
    {
        _parserService = parserService;
        _dbContext = dbContext;
        _embeddingService = embeddingService;
    }

    public async Task<IngestDocumentResponse> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
    {
        var ext = System.IO.Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".md" && ext != ".txt")
        {
            throw new ArgumentException("Unsupported file extension. Only .pdf, .md, and .txt are supported.");
        }

        using var stream = request.File.OpenReadStream();
        
        var chunks = await _parserService.ParseAsync(stream, request.File.FileName, request.DocumentType, request.ExternalReferenceId, cancellationToken);
        
        if (!chunks.Any())
        {
            throw new InvalidOperationException("No text could be extracted from the file.");
        }

        // Generate embeddings for all chunks
        var texts = chunks.Select(c => c.TextContent).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].EmbeddingJson = System.Text.Json.JsonSerializer.Serialize(embeddings[i]);
        }

        _dbContext.DocumentChunks.AddRange(chunks);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new IngestDocumentResponse("Document ingested successfully.", chunks.Count);
    }
}
