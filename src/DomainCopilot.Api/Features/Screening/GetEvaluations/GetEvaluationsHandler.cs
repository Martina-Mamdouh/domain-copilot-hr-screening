using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Api.Features.Screening.GetEvaluations;

public class GetEvaluationsHandler : IRequestHandler<GetEvaluationsQuery, List<EvaluationDto>>
{
    private readonly AppDbContext _dbContext;

    public GetEvaluationsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<EvaluationDto>> Handle(GetEvaluationsQuery request, CancellationToken cancellationToken)
    {
        var evaluations = await _dbContext.CandidateEvaluations
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(50)
            .Select(e => new EvaluationDto(
                e.Id,
                e.CandidateAlias,
                e.WeightedScore,
                e.RecommendedDecision,
                e.Status,
                e.CreatedAtUtc,
                e.CompetencyBreakdownJson
            ))
            .ToListAsync(cancellationToken);

        return evaluations;
    }
}
