using System.Threading;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Core.Interfaces;

public interface IGroundedGenerationService
{
    Task<GroundedAnswerDto> GenerateAnswerAsync(string query, string? categoryFilter = null, CancellationToken cancellationToken = default);
}
