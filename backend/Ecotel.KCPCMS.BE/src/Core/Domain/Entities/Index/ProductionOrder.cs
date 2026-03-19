using Domain.Common.Contracts;
using Shared.Constants;

namespace Domain.Entities.Index;

public class ProductionOrder : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; protected set; }

    public static ProductionOrder Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.ProductionOrderValueNullOrEmpty);
        }

        return new ProductionOrder
        {
            Value = value
        };
    }

    public void Update(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.ProductionOrderValueNullOrEmpty);
        }
        Value = value;
    }

    public bool CheckChange(ProductionOrder dto)
    {
        return !(Value == dto.Value);
    }
}
