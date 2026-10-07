using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Corpus.Search;

public class SearchEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/corpus/search", async (
            [FromQuery] string query,
            [FromQuery] int topK,
            [FromQuery] float similarityThreshold,
            ISender sender) =>
        {
            var command = new SearchQuery(query, topK == 0 ? 5 : topK, similarityThreshold == 0 ? 0.7f : similarityThreshold);
            var response = await sender.Send(command);
            return Results.Ok(response);
        })
        .WithTags("Corpus")
        .RequireAuthorization();
    }
}
