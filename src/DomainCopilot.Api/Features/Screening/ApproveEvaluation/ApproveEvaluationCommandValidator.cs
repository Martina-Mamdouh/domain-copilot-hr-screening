using FluentValidation;

namespace DomainCopilot.Api.Features.Screening.ApproveEvaluation;

public class ApproveEvaluationCommandValidator : AbstractValidator<ApproveEvaluationCommand>
{
    public ApproveEvaluationCommandValidator()
    {
        RuleFor(x => x.EvaluationId)
            .NotEmpty().WithMessage("Evaluation ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}
