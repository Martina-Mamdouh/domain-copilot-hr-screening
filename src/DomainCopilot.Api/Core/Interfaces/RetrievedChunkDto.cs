using System;

namespace DomainCopilot.Api.Core.Interfaces;

public class RetrievedChunkDto
{
    public Guid ChunkId { get; set; }
    public string DocId { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}
