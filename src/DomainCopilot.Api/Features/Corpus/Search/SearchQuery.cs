using MediatR;
using System.Collections.Generic;

namespace DomainCopilot.Api.Features.Corpus.Search;

public record SearchQuery(string Query, string? Category = null, int TopK = 5, float SimilarityThreshold = 0.7f) : IRequest<SearchResponse>;

public record SearchResponse(string Query, List<SearchResult> Results);

public record SearchResult(string Content, string DocId, string DocumentType, string SectionTitle, float SimilarityScore);
