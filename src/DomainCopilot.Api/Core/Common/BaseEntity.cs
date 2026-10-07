using System;

namespace DomainCopilot.Api.Core.Common;

public abstract class BaseEntity<TId>
{
    public TId Id { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public abstract class BaseEntity : BaseEntity<Guid>
{
    public BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
