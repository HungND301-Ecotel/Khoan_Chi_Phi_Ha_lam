using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class Hardness : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Value { get; protected set; }

        //Navigation Properties
        private IList<StoneClampRatio> _stoneClampRatios = new List<StoneClampRatio>();
        public virtual IReadOnlyCollection<StoneClampRatio> StoneClampRatios => _stoneClampRatios.AsReadOnly();

        private IList<TunnelExcavationMaterialUnitPrice> _materialUnitPrices = new List<TunnelExcavationMaterialUnitPrice>();
        public virtual IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();
        private IList<SlideUnitPrice> _slideUnitPrices = new List<SlideUnitPrice>();
        public virtual IReadOnlyCollection<SlideUnitPrice> SlideUnitPrices => _slideUnitPrices.AsReadOnly();

        public static Hardness Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(CustomResponseMessage.HardnessValueNullOrEmpty);
            }

            return new Hardness
            {
                Value = value
            };
        }

        public void Update(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(CustomResponseMessage.HardnessValueNullOrEmpty);
            }
            Value = value;
        }

        public bool CheckChange(Hardness dto)
        {
            return !(Value == dto.Value);
        }
    }
}
