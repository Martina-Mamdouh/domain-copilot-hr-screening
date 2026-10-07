using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using DomainCopilot.Api.Core.Enums;
using DomainCopilot.Api.Features.Screening.ReviewCandidate;
using DomainCopilot.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DomainCopilot.Tests.Features.Screening;

public class SubmitReviewHandlerTests
{
    private readonly AppDbContext _dbContext;
    private readonly SubmitReviewHandler _handler;

    public SubmitReviewHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);
        _handler = new SubmitReviewHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_WhenApprovalMatchesAi_ShouldUpdateEvaluationAndAddAuditLog()
    {
        // Arrange
        var evaluation = new CandidateEvaluation
        {
            Id = Guid.NewGuid(),
            Status = ReviewStatus.PendingHumanApproval,
            RecommendedDecision = ScreeningDecision.Shortlist
        };
        _dbContext.CandidateEvaluations.Add(evaluation);
        await _dbContext.SaveChangesAsync();

        var command = new SubmitReviewCommand(evaluation.Id, ReviewStatus.Approved, "TestManager", null, "Looks good", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ReviewStatus.Approved);
        result.ReviewedBy.Should().Be("TestManager");
        result.ManagerReviewerNotes.Should().Be("Looks good");
        
        var audit = await _dbContext.AuditLogs.SingleOrDefaultAsync();
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("HUMAN_REVIEW_DECISION");
        audit.PerformedBy.Should().Be("TestManager");
    }

    [Fact]
    public async Task Handle_WhenOverridingAiWithReason_ShouldUpdateAndLogOverride()
    {
        // Arrange
        var evaluation = new CandidateEvaluation
        {
            Id = Guid.NewGuid(),
            Status = ReviewStatus.PendingHumanApproval,
            RecommendedDecision = ScreeningDecision.Shortlist
        };
        _dbContext.CandidateEvaluations.Add(evaluation);
        await _dbContext.SaveChangesAsync();

        var command = new SubmitReviewCommand(evaluation.Id, ReviewStatus.Overridden, "TestManager", null, "Bad fit", "Lacks key experience not caught by AI");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ReviewStatus.Overridden);
        result.ManagerOverrideReason.Should().Be("Lacks key experience not caught by AI");

        var audit = await _dbContext.AuditLogs.SingleOrDefaultAsync();
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("HUMAN_OVERRIDE");
    }

    [Fact]
    public async Task Handle_WhenOverridingAiWithoutReason_ShouldThrowArgumentException()
    {
        // Arrange
        var evaluation = new CandidateEvaluation
        {
            Id = Guid.NewGuid(),
            Status = ReviewStatus.PendingHumanApproval,
            RecommendedDecision = ScreeningDecision.Shortlist
        };
        _dbContext.CandidateEvaluations.Add(evaluation);
        await _dbContext.SaveChangesAsync();

        var command = new SubmitReviewCommand(evaluation.Id, ReviewStatus.Overridden, "TestManager", null, "Bad fit", "");

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("An override reason is mandatory when deviating from AI recommendation.");
    }

    [Fact]
    public async Task Handle_WhenEvaluationAlreadyProcessed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var evaluation = new CandidateEvaluation
        {
            Id = Guid.NewGuid(),
            Status = ReviewStatus.Approved,
            RecommendedDecision = ScreeningDecision.Shortlist
        };
        _dbContext.CandidateEvaluations.Add(evaluation);
        await _dbContext.SaveChangesAsync();

        var command = new SubmitReviewCommand(evaluation.Id, ReviewStatus.Approved, "TestManager", null, "Looks good", null);

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Evaluation is not pending approval");
    }
}
