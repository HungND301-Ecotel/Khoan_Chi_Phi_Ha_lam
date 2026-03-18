using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class InsertItem : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Value { get; protected set; }

        //Navigation Properties
        private IList<TunnelExcavationMaterialUnitPrice> _materialUnitPrices = new List<TunnelExcavationMaterialUnitPrice>();
        public virtual IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

        public static InsertItem Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(CustomResponseMessage.InsertItemValueNullOrEmpty);
            }

            return new InsertItem
            {
                Value = value
            };
        }

        public void Update(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(CustomResponseMessage.InsertItemValueNullOrEmpty);
            }
            Value = value;
        }

        public bool CheckChange(InsertItem dto)
        {
            return !(Value == dto.Value);
        }
    }
}
