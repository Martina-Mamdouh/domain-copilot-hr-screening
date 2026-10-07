using System;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Core.Enums;
using MediatR;

namespace DomainCopilot.Api.Features.Screening.ApproveEvaluation;

public record ApproveEvaluationCommand(Guid EvaluationId, bool IsApproved, string? Notes) : IRequest<CandidateEvaluation>;
