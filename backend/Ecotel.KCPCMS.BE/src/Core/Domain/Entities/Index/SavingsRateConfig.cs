using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class SavingsRateConfig : AuditableEntity<Guid>, IAggregateRoot
{
    public decimal? MaxRevenue { get; protected set; }
    public decimal? MaxSavingsRate { get; protected set; }
    public string? Description { get; protected set; }

    public static SavingsRateConfig Create(decimal? maxRevenue, decimal? maxSavingsRate, string? description)
    {
        return new SavingsRateConfig
        {
            MaxRevenue = maxRevenue,
            MaxSavingsRate = maxSavingsRate,
            Description = description
        };
    }

    public void Update(decimal? maxRevenue, decimal? maxSavingsRate, string? description)
    {
        MaxRevenue = maxRevenue;
        MaxSavingsRate = maxSavingsRate;
        Description = description;
    }
}