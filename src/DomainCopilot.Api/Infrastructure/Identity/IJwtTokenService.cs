using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;

namespace DomainCopilot.Api.Infrastructure.Identity;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(ApplicationUser user);
}
