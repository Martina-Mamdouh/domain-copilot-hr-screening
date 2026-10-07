using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Core.Enums;
using DomainCopilot.Api.Infrastructure.Persistence;
using MediatR;

namespace DomainCopilot.Api.Features.Screening.ApproveEvaluation;

public class ApproveEvaluationHandler : IRequestHandler<ApproveEvaluationCommand, CandidateEvaluation>
{
    private readonly AppDbContext _dbContext;

    public ApproveEvaluationHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CandidateEvaluation> Handle(ApproveEvaluationCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _dbContext.CandidateEvaluations.FindAsync(new object[] { request.EvaluationId }, cancellationToken);

        if (evaluation == null)
        {
            throw new ArgumentException("Evaluation not found");
        }

        if (evaluation.Status != ReviewStatus.PendingHumanApproval)
        {
            throw new InvalidOperationException("Evaluation is not pending approval");
        }

        evaluation.Status = request.IsApproved ? ReviewStatus.Approved : ReviewStatus.Overridden;
        evaluation.ManagerReviewerNotes = request.Notes;
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return evaluation;
    }
}
