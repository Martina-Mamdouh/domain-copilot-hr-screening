using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace DomainCopilot.Api.Features.Corpus.IngestDocument;

using Microsoft.AspNetCore.Mvc;

public record IngestDocumentResponse(string Message, int TotalChunks);
public record IngestDocumentCommand(
    IFormFile File, 
    string DocumentType, 
    string? ExternalReferenceId
) : IRequest<IngestDocumentResponse>;
