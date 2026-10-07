using System;
using System.Security.Claims;
using DomainCopilot.Api.Common;
using DomainCopilot.Api.Core.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Screening.ReviewCandidate;

public class SubmitReviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/screenings/{id:guid}/review", async (
            Guid id,
            [FromBody] SubmitReviewRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            var reviewerName = httpContext.User.Identity?.Name 
                ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? request.ReviewerName 
                ?? "System Admin";

            Guid? reviewerUserId = null;
            var nameIdentifier = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(nameIdentifier, out var parsedId))
            {
                reviewerUserId = parsedId;
            }

            var command = new SubmitReviewCommand(
                id, 
                request.FinalStatus, 
                reviewerName, 
                reviewerUserId,
                request.Comments, 
                request.OverrideReason);

            try
            {
                var response = await sender.Send(command);
                return Results.Ok(response);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithTags("Screening")
        .RequireAuthorization(policy => policy.RequireRole("HiringManager", "Admin"));
    }
}

public record SubmitReviewRequest(
    ReviewStatus FinalStatus,
    string? ReviewerName,
    string? Comments,
    string? OverrideReason
);
