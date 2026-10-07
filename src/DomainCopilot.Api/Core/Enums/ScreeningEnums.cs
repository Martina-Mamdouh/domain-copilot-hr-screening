namespace DomainCopilot.Api.Core.Enums;

public enum ScreeningDecision
{
    Shortlist = 1,
    Hold = 2,
    Reject = 3
}

public enum ReviewStatus
{
    PendingHumanApproval = 1,
    Approved = 2,
    Overridden = 3,
    Rejected = 4
}

public enum DocumentCategory
{
    JobDescription,
    EvaluationRubric,
    CandidateProfile
}
