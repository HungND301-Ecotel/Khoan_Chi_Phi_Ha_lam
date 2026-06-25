namespace Domain.Common.Contracts;

public abstract class AuditableEntity : AuditableEntity<Guid>;

public abstract class BasicAuditableEntity<T> : BaseEntity<T>, IAuditableEntity
{
    public long CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; private set; } = DateTimeOffset.UtcNow;

    public long LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; } = DateTimeOffset.UtcNow;
}

public abstract class AuditableEntity<T> : BaseEntity<T>, IAuditableEntity, ISoftDelete
{
    public long CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; private set; } = DateTimeOffset.UtcNow;
    public long LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedOn { get; set; }
    public long? DeletedBy { get; set; }

    public void Restore()
    {
        DeletedOn = null;
        DeletedBy = null;
    }
}