using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Core.Enums;
using DomainCopilot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace DomainCopilot.Api.Features.Screening;

public class EvaluateCandidateHandler : IRequestHandler<EvaluateCandidateCommand, CandidateEvaluation>
{
    private readonly AppDbContext _dbContext;
    private readonly Kernel _kernel;

    public EvaluateCandidateHandler(AppDbContext dbContext, Kernel kernel)
    {
        _dbContext = dbContext;
        _kernel = kernel;
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

        // 2. Extractor Agent
        var extractorPrompt = @"You are an expert HR Extraction Agent. 
Extract the key skills, years of experience, and educational background from the candidate's resume below.
Return a concise summary.

Candidate Resume:
{{$candidate_resume}}";

        var extractionResult = await _kernel.InvokePromptAsync(extractorPrompt, new KernelArguments { ["candidate_resume"] = candidateContent });

        // 3. Evaluator Agent
        var evaluatorPrompt = @"You are an expert HR Evaluation Agent. 
Compare the extracted candidate profile with the Job Description.
Output a JSON response with two properties:
- ""Score"": an integer from 0 to 100 representing the fit.
- ""Decision"": either ""Shortlist"", ""Hold"", or ""Reject"".

Extracted Candidate Profile:
{{$candidate_profile}}

Job Description:
{{$job_description}}";

        var evaluationResult = await _kernel.InvokePromptAsync(evaluatorPrompt, new KernelArguments 
        { 
            ["candidate_profile"] = extractionResult.ToString(),
            ["job_description"] = jdContent
        });

        var evalText = evaluationResult.ToString();
        var score = 50.0;
        var decision = ScreeningDecision.Hold;

        if (evalText.Contains("Shortlist", StringComparison.OrdinalIgnoreCase)) decision = ScreeningDecision.Shortlist;
        else if (evalText.Contains("Reject", StringComparison.OrdinalIgnoreCase)) decision = ScreeningDecision.Reject;

        var evaluation = new CandidateEvaluation
        {
            CandidateDocId = request.CandidateDocId,
            TargetJdId = request.TargetJdId,
            CandidateAlias = "Candidate-" + Guid.NewGuid().ToString().Substring(0, 4),
            WeightedScore = score, // In real scenario, parse JSON for score
            RecommendedDecision = decision,
            Status = ReviewStatus.PendingHumanApproval,
            CompetencyBreakdownJson = extractionResult.ToString() // Storing the extraction result here
        };

        _dbContext.CandidateEvaluations.Add(evaluation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return evaluation;
    }
}
