using DomainCopilot.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Features.Auth.Login;

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async ([FromBody] LoginCommand command, ISender sender) =>
        {
            return await sender.Send(command);
        })
        .WithTags("Authentication")
        .AddEndpointFilter<ValidationFilter<LoginCommand>>()
        .AllowAnonymous();
    }
}
