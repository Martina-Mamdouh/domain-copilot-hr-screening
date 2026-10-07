using MediatR;
using Microsoft.AspNetCore.Http;

namespace DomainCopilot.Api.Features.Auth.GetCurrentUser;

public record GetCurrentUserQuery(string UserId) : IRequest<IResult>;
