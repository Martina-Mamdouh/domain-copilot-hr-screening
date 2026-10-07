using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace DomainCopilot.Api.Features.Corpus.IngestDocument;

using Microsoft.AspNetCore.Mvc;

public record IngestDocumentResponse(string Message, int TotalChunks);
public record IngestDocumentCommand(
    [FromForm] IFormFile File, 
    [FromForm] string DocumentType, 
    [FromForm] string? ExternalReferenceId
) : IRequest<IngestDocumentResponse>;
