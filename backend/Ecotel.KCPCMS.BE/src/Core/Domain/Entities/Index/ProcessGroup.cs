using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class ProcessGroup : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public Guid? FixedKeyId { get; protected set; }

        public ProcessGroupType Type { get; protected set; } = ProcessGroupType.None;

        // Navigation properties
        public virtual Code? Code { get; protected set; }
        public virtual FixedKey? FixedKey { get; protected set; }

        private IList<ProductionProcess> _productionProcesses = new List<ProductionProcess>();
        public virtual IReadOnlyCollection<ProductionProcess> ProductionProcesses => _productionProcesses.AsReadOnly();

        private IList<AdjustmentFactor> _adjustmentFactors = new List<AdjustmentFactor>();
        public IReadOnlyList<AdjustmentFactor> AdjustmentFactors => _adjustmentFactors.ToList();

        private IList<LowValuePerishableSupplyUnitPrice> _lowValuePerishableSupplyUnitPrices = new List<LowValuePerishableSupplyUnitPrice>();
        public IReadOnlyList<LowValuePerishableSupplyUnitPrice> LowValuePerishableSupplyUnitPrices => _lowValuePerishableSupplyUnitPrices.ToList();

        private IList<Product> _products = new List<Product>();
        public IReadOnlyList<Product> Products => _products.ToList();

        private IList<SlideUnitPrice> _slideUnitPrices = new List<SlideUnitPrice>();
        public virtual IReadOnlyCollection<SlideUnitPrice> SlideUnitPrices => _slideUnitPrices.AsReadOnly();
        private IList<PartProcessGroup> _partProcessGroups = new List<PartProcessGroup>();
        public virtual IReadOnlyCollection<PartProcessGroup> PartProcessGroups => _partProcessGroups.AsReadOnly();

        // constructor
        public static ProcessGroup Create(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new ProcessGroup
            {
                Code = new Code(code.ToUpper()),
                Name = name,
            };
        }

        public static ProcessGroup Create(Guid id, string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new ProcessGroup
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
            };
        }

        public static ProcessGroup Create(FixedKey fixedKey, string name)
        {
            ArgumentNullException.ThrowIfNull(fixedKey);

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new ProcessGroup
            {
                Code = new Code(fixedKey.Key),
                FixedKeyId = fixedKey.Id,
                FixedKey = fixedKey,
                Name = name,
                Type = fixedKey.Type,
            };
        }

        public void Update(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            if (Code != null)
            {
                Code.Value = code.ToUpper();
            }

            Name = name;
        }

        public void Update(FixedKey fixedKey, string name)
        {
            ArgumentNullException.ThrowIfNull(fixedKey);

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            if (Code != null)
            {
                Code.Value = fixedKey.Key;
            }
            else
            {
                Code = new Code(fixedKey.Key);
            }

            FixedKeyId = fixedKey.Id;
            FixedKey = fixedKey;
            Name = name;
            Type = fixedKey.Type;
        }

        public bool CheckChange(ProcessGroup dto)
        {
            return !(Name == dto.Name && Code?.Value == dto.Code?.Value);
        }
    }
}
