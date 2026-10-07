using System.Threading;
using System.Threading.Tasks;

namespace DomainCopilot.Api.Features.Screening.Services;

public record AgentOneResult(string ExtractedSkills, bool MeetsMinimumRequirements);
public record AgentTwoResult(string SanitizedCv, bool InjectionDetected);
public record AgentThreeResult(int Score, string Recommendation, string Reasoning);

public record ScreeningResult(
    AgentOneResult AgentOne, 
    AgentTwoResult AgentTwo, 
    AgentThreeResult AgentThree,
    System.Collections.Generic.List<DomainCopilot.Api.Core.Entities.AgentExecutionTrace> Traces);

public interface IScreeningPipelineService
{
    Task<ScreeningResult> RunPipelineAsync(string rawCvText, string jobDescription, CancellationToken cancellationToken = default);
}
