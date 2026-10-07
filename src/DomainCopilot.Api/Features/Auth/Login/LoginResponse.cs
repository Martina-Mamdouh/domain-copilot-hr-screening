using System.Collections.Generic;

namespace DomainCopilot.Api.Features.Auth.Login;

public record LoginResponse(string Token, string Email, string FullName, IList<string> Roles);
