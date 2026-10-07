using FluentValidation;

namespace DomainCopilot.Api.Features.Screening.EvaluateCandidate;

public class EvaluateCandidateCommandValidator : AbstractValidator<EvaluateCandidateCommand>
{
    public EvaluateCandidateCommandValidator()
    {
        RuleFor(x => x.CandidateDocId)
            .NotEmpty().WithMessage("Candidate Document ID is required.");

        RuleFor(x => x.TargetJdId)
            .NotEmpty().WithMessage("Target Job Description ID is required.");
    }
}
