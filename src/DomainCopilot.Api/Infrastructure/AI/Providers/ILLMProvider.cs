using System.Threading;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Infrastructure.AI.Providers;

public interface ILLMProvider
{
    string ProviderName { get; }
    Task<string> GenerateTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
