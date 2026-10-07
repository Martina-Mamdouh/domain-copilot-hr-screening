using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Core.Enums;
using DomainCopilot.Api.Features.Screening.Services;
using DomainCopilot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Api.Features.Screening.EvaluateCandidate;

public class EvaluateCandidateHandler : IRequestHandler<EvaluateCandidateCommand, CandidateEvaluation>
{
    private readonly AppDbContext _dbContext;
    private readonly IScreeningPipelineService _pipelineService;

    public EvaluateCandidateHandler(AppDbContext dbContext, IScreeningPipelineService pipelineService)
    {
        _dbContext = dbContext;
        _pipelineService = pipelineService;
    }

    public async Task<CandidateEvaluation> Handle(EvaluateCandidateCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch Candidate and JD content
        var candidateChunks = await _dbContext.DocumentChunks
            .Where(c => c.DocId == request.CandidateDocId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);

        var jdChunks = await _dbContext.DocumentChunks
            .Where(c => c.DocId == request.TargetJdId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);

        if (!candidateChunks.Any() || !jdChunks.Any())
        {
            throw new InvalidOperationException("Candidate or JD documents not found.");
        }

        var candidateContent = string.Join("\n", candidateChunks.Select(c => c.Content));
        var jdContent = string.Join("\n", jdChunks.Select(c => c.Content));

        // 2. Run the 3-Agent Pipeline
        var pipelineResult = await _pipelineService.RunPipelineAsync(candidateContent, jdContent, cancellationToken);

        // 3. Map Pipeline Result to Entity
        var decision = ScreeningDecision.Hold;
        if (pipelineResult.AgentThree.Recommendation.Contains("HIRE") || pipelineResult.AgentThree.Recommendation.Contains("SHORTLIST")) 
            decision = ScreeningDecision.Shortlist;
        else if (pipelineResult.AgentThree.Recommendation.Contains("REJECT")) 
            decision = ScreeningDecision.Reject;

        var evaluation = new CandidateEvaluation
        {
            CandidateDocId = request.CandidateDocId,
            TargetJdId = request.TargetJdId,
            CandidateAlias = "Candidate-" + Guid.NewGuid().ToString().Substring(0, 4),
            WeightedScore = pipelineResult.AgentThree.Score,
            RecommendedDecision = decision,
            Status = ReviewStatus.PendingHumanApproval,
            CompetencyBreakdownJson = $"Skills: {pipelineResult.AgentOne.ExtractedSkills}\n\nReasoning: {pipelineResult.AgentThree.Reasoning}"
        };

        _dbContext.CandidateEvaluations.Add(evaluation);
        
        // Link and save traces
        foreach (var trace in pipelineResult.Traces)
        {
            trace.CandidateEvaluationId = evaluation.Id;
            trace.Evaluation = evaluation;
            _dbContext.AgentExecutionTraces.Add(trace);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return evaluation;
    }
}
