using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Interfaces;
using MediatR;

namespace DomainCopilot.Api.Features.Corpus.Search;

public class SearchHandler : IRequestHandler<SearchQuery, SearchResponse>
{
    private readonly IHybridRetrievalService _retrievalService;

    public SearchHandler(IHybridRetrievalService retrievalService)
    {
        _retrievalService = retrievalService;
    }

    public async Task<SearchResponse> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        var chunks = await _retrievalService.RetrieveRelevantChunksAsync(
            request.Query, 
            request.Category, 
            request.TopK, 
            cancellationToken);

        var results = chunks.Select(c => 
            new SearchResult(c.Content, c.DocId, request.Category ?? "Unknown", c.SectionTitle, (float)c.RelevanceScore))
            .ToList();

        return new SearchResponse(request.Query, results);
    }
}
