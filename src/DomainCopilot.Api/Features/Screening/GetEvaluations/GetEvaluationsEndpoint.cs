using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Screening.GetEvaluations;

public class GetEvaluationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/screening/evaluations", async (ISender sender) =>
        {
            var evaluations = await sender.Send(new GetEvaluationsQuery());
            return Results.Ok(evaluations);
        })
        .WithTags("Screening")
        .RequireAuthorization();
    }
}
