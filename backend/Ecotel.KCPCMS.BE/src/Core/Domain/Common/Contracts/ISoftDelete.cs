namespace Domain.Common.Contracts;

public interface ISoftDelete
{
    DateTimeOffset? DeletedOn { get; set; }
    long? DeletedBy { get; set; }
}