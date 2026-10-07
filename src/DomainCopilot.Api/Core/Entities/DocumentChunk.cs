using DomainCopilot.Api.Core.Common;

namespace DomainCopilot.Api.Core.Entities;

public class DocumentChunk : BaseEntity
{
    public string DocId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? EmbeddingJson { get; set; }
    
    public int? PageNumber { get; set; }
    public int ChunkIndex { get; set; }
    public int WordCount { get; set; }
}
