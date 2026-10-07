using System.Security.Claims;
using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Auth.GetCurrentUser;

public class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", async (ClaimsPrincipal userPrincipal, ISender sender) =>
        {
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetCurrentUserQuery(userId);
            return await sender.Send(query);
        })
        .WithTags("Authentication")
        .RequireAuthorization();
    }
}
