using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Core.Enums;
using DomainCopilot.Api.Infrastructure.Persistence;
using MediatR;

namespace DomainCopilot.Api.Features.Screening.ReviewCandidate;

public class SubmitReviewHandler : IRequestHandler<SubmitReviewCommand, CandidateEvaluation>
{
    private readonly AppDbContext _dbContext;

    public SubmitReviewHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CandidateEvaluation> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _dbContext.CandidateEvaluations.FindAsync(new object[] { request.EvaluationId }, cancellationToken);
        if (evaluation == null)
        {
            throw new KeyNotFoundException("Evaluation not found");
        }

        if (evaluation.Status != ReviewStatus.PendingHumanApproval)
        {
            throw new InvalidOperationException("Evaluation is not pending approval");
        }

        bool isOverride = 
            (evaluation.RecommendedDecision == ScreeningDecision.Shortlist && (request.FinalStatus == ReviewStatus.Rejected || request.FinalStatus == ReviewStatus.Overridden)) ||
            (evaluation.RecommendedDecision == ScreeningDecision.Reject && request.FinalStatus == ReviewStatus.Approved);

        if (isOverride && string.IsNullOrWhiteSpace(request.OverrideReason))
        {
            throw new ArgumentException("An override reason is mandatory when deviating from AI recommendation.");
        }

        evaluation.Status = request.FinalStatus;
        evaluation.ReviewedBy = request.ReviewerName;
        evaluation.ReviewedByUserId = request.ReviewerUserId;
        evaluation.ReviewedAt = DateTime.UtcNow;
        evaluation.ManagerReviewerNotes = request.Comments;
        if (isOverride)
        {
            evaluation.ManagerOverrideReason = request.OverrideReason;
        }

        if (request.EditedScore.HasValue)
        {
            evaluation.WeightedScore = request.EditedScore.Value;
        }
        if (request.EditedProbes != null)
        {
            evaluation.InterviewProbes = request.EditedProbes;
        }

        var auditLog = new AuditLogEntry
        {
            CandidateEvaluationId = evaluation.Id,
            Action = isOverride ? "HUMAN_OVERRIDE" : "HUMAN_REVIEW_DECISION",
            PerformedBy = request.ReviewerName,
            PerformedByUserId = request.ReviewerUserId,
            Details = $"AI: {evaluation.RecommendedDecision}, Manager: {request.FinalStatus}. Comments: {request.Comments}. OverrideReason: {request.OverrideReason}",
            Timestamp = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return evaluation;
    }
}
