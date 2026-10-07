using Microsoft.AspNetCore.Routing;

namespace DomainCopilot.Api.Common;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
