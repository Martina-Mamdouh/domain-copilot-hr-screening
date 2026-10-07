using System.Collections.Generic;

namespace DomainCopilot.Api.Features.Auth.GetCurrentUser;

public record UserProfileResponse(string Id, string Email, string FullName, string Department, IList<string> Roles);
