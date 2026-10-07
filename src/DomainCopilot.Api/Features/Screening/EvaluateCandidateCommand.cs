using System;
using DomainCopilot.Api.Core.Entities;
using MediatR;

namespace DomainCopilot.Api.Features.Screening;

public record EvaluateCandidateCommand(string CandidateDocId, string TargetJdId) : IRequest<CandidateEvaluation>;
