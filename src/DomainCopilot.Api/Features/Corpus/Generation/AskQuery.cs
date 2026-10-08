using DomainCopilot.Api.Core.Interfaces;
using MediatR;

namespace DomainCopilot.Api.Features.Corpus.Generation;

public record AskQuery(string Query, string? Category = null) : IRequest<GroundedAnswerDto>;
