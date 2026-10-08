using System.Threading;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public interface IResilientLLMService
{
    Task<(LLMResult Result, string ProviderUsed)> GenerateTextWithFallbackAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
