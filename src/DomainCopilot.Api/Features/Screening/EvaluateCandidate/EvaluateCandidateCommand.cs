using System;
using DomainCopilot.Api.Core.Entities;
using MediatR;

namespace DomainCopilot.Api.Features.Screening.EvaluateCandidate;

public record EvaluateCandidateCommand(string CandidateDocId, string TargetJdId) : IRequest<CandidateEvaluation>;
