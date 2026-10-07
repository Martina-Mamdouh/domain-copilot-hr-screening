using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Screening.EvaluateCandidate;

public class EvaluateCandidateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/screening/evaluate", async (
            [FromBody] EvaluateCandidateCommand command,
            ISender sender) =>
        {
            var response = await sender.Send(command);
            return Results.Ok(response);
        })
        .AddEndpointFilter<DomainCopilot.Api.Common.ValidationFilter<EvaluateCandidateCommand>>()
        .WithTags("Screening")
        .RequireAuthorization();
    }
}
