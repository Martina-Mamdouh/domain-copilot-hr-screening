using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Features.Screening;

public class ScreeningEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // FR-3: Agentic Extractor & Evaluator
        app.MapPost("/api/screening/evaluate", async (
            [FromBody] EvaluateCandidateCommand command,
            ISender sender) =>
        {
            var response = await sender.Send(command);
            return Results.Ok(response);
        })
        .WithTags("Screening")
        .RequireAuthorization();

        // FR-4: Approval Workflow
        app.MapPost("/api/screening/approve/{evaluationId}", async (
            Guid evaluationId,
            [FromBody] ApproveEvaluationCommand command,
            ISender sender) =>
        {
            if (evaluationId != command.EvaluationId) 
                return Results.BadRequest("ID mismatch");

            var response = await sender.Send(command);
            return Results.Ok(response);
        })
        .WithTags("Screening")
        .RequireAuthorization();
    }
}
