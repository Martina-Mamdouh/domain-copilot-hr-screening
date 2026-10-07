using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace DomainCopilot.Api.Features.Corpus.IngestDocument;

public class IngestDocumentCommandValidator : AbstractValidator<IngestDocumentCommand>
{
    public IngestDocumentCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(f => f != null && f.Length > 0).WithMessage("File cannot be empty.")
            .Must(f => f != null && f.Length <= 10 * 1024 * 1024).WithMessage("File size must not exceed 10 MB.");

        RuleFor(x => x.DocumentType)
            .NotEmpty().WithMessage("Document Type is required.");
    }
}
