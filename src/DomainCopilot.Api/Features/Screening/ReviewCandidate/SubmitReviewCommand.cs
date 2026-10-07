using System;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Core.Enums;
using MediatR;

namespace DomainCopilot.Api.Features.Screening.ReviewCandidate;

public record SubmitReviewCommand(
    Guid EvaluationId, 
    ReviewStatus FinalStatus, 
    string ReviewerName, 
    Guid? ReviewerUserId,
    string? Comments, 
    string? OverrideReason) : IRequest<CandidateEvaluation>;
