using System;
using System.Collections.Generic;
using DomainCopilot.Api.Core.Enums;
using MediatR;

namespace DomainCopilot.Api.Features.Screening.GetEvaluations;

public record GetEvaluationsQuery() : IRequest<List<EvaluationDto>>;

public record EvaluationDto(
    Guid Id,
    string CandidateAlias,
    double WeightedScore,
    ScreeningDecision RecommendedDecision,
    ReviewStatus Status,
    DateTime CreatedAtUtc,
    string CompetencyBreakdownJson
);
