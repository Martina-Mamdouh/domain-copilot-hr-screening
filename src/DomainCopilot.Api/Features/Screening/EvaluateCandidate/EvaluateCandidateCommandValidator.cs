using FluentValidation;

namespace DomainCopilot.Api.Features.Screening.EvaluateCandidate;

public class EvaluateCandidateCommandValidator : AbstractValidator<EvaluateCandidateCommand>
{
    public EvaluateCandidateCommandValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrEmpty(x.CandidateDocId) || !string.IsNullOrEmpty(x.RawCvText))
            .WithMessage("Either CandidateDocId or RawCvText is required.");

        RuleFor(x => x).Must(x => !string.IsNullOrEmpty(x.TargetJdId) || !string.IsNullOrEmpty(x.JobDescription))
            .WithMessage("Either TargetJdId or JobDescription is required.");
    }
}
