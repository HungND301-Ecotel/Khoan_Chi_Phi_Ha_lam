using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class SupportStep : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Value { get; protected set; }

        //Navigation Properties
        private IList<TunnelExcavationMaterialUnitPrice> _materialUnitPrices = new List<TunnelExcavationMaterialUnitPrice>();
        public virtual IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

        public static SupportStep Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(CustomResponseMessage.SupportStepValueNullOrEmpty);
            }

            return new SupportStep
            {
                Value = value
            };
        }

        public void Update(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(CustomResponseMessage.SupportStepValueNullOrEmpty);
            }
            Value = value;
        }

        public bool CheckChange(SupportStep dto)
        {
            return !(Value == dto.Value);
        }
    }
}
