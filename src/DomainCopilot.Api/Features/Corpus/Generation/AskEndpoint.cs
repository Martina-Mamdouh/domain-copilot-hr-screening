using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Corpus.Generation;

public class AskEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/retrieval/ask", async (
            [FromBody] AskQuery command,
            ISender sender) =>
        {
            var response = await sender.Send(command);
            return Results.Ok(response);
        })
        .WithTags("Retrieval")
        .RequireAuthorization();
    }
}
