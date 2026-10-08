using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Interfaces;
using MediatR;

namespace DomainCopilot.Api.Features.Corpus.Generation;

public class AskQueryHandler : IRequestHandler<AskQuery, GroundedAnswerDto>
{
    private readonly IGroundedGenerationService _generationService;

    public AskQueryHandler(IGroundedGenerationService generationService)
    {
        _generationService = generationService;
    }

    public async Task<GroundedAnswerDto> Handle(AskQuery request, CancellationToken cancellationToken)
    {
        return await _generationService.GenerateAnswerAsync(request.Query, request.Category, cancellationToken);
    }
}
