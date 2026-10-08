using System;
using DomainCopilot.Api.Core.Entities;
using MediatR;

namespace DomainCopilot.Api.Features.Screening.EvaluateCandidate;

public record EvaluateCandidateCommand(
    string? CandidateDocId = null, 
    string? TargetJdId = null,
    string? RawCvText = null,
    string? JobDescription = null) : IRequest<CandidateEvaluation>;
