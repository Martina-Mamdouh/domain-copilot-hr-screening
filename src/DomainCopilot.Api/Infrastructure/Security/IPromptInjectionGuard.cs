namespace DomainCopilot.Api.Infrastructure.Security;

public interface IPromptInjectionGuard
{
    bool IsPotentiallyMalicious(string input);
    string Sanitize(string input);
}
