using MediatR;
using Microsoft.AspNetCore.Http;

namespace DomainCopilot.Api.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : IRequest<IResult>;
