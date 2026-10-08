using System.Collections.Generic;

namespace DomainCopilot.Api.Core.Interfaces;

public class GroundedAnswerDto
{
    public string Answer { get; set; } = string.Empty;
    public IReadOnlyList<RetrievedChunkDto> Sources { get; set; } = new List<RetrievedChunkDto>();
    public bool IsGrounded { get; set; }
}
