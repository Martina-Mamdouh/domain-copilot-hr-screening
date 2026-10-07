using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace DomainCopilot.Api.Features.Corpus.IngestDocument;

public record IngestDocumentResponse(string Message, int TotalChunks);
public record IngestDocumentCommand(IFormFile File, string DocumentType, string? ExternalReferenceId) : IRequest<IngestDocumentResponse>;
