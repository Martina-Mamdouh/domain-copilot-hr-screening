using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;

namespace DomainCopilot.Api.Features.Corpus.Services;

public interface IDocumentParserService
{
    Task<List<DocumentChunk>> ParseAsync(Stream fileStream, string fileName, string documentType, string? docId, CancellationToken cancellationToken = default);
}
