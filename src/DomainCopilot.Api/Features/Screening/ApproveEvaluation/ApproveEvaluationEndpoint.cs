using System;
using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Screening.ApproveEvaluation;

public class ApproveEvaluationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
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
        .AddEndpointFilter<DomainCopilot.Api.Common.ValidationFilter<ApproveEvaluationCommand>>()
        .WithTags("Screening")
        .RequireAuthorization();
    }
}
