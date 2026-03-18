namespace Domain.Common.Contracts;

public interface IAuditableEntity
{
    public long CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; }
    public long LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }
}