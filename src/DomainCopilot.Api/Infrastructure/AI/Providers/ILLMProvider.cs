using System.Threading;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public record LLMResult(string Text, int? PromptTokens, int? CompletionTokens);

public interface ILLMProvider
{
    string ProviderName { get; }
    Task<LLMResult> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
