using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class ProductionProcess : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public Guid ProcessGroupId { get; protected set; }
        public Guid? UnitOfMeasureId { get; protected set; }

        // Navigation properties
        public virtual Code? Code { get; protected set; }
        public virtual ProcessGroup? ProcessGroup { get; protected set; }
        public virtual UnitOfMeasure? UnitOfMeasure { get; protected set; }

        private IList<StoneClampRatio> _stoneClampRatios = new List<StoneClampRatio>();
        public virtual IReadOnlyCollection<StoneClampRatio> StoneClampRatios => _stoneClampRatios.AsReadOnly();

        private IList<MaterialUnitPrice> _materialUnitPrices = new List<MaterialUnitPrice>();
        public virtual IReadOnlyCollection<MaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

        private IList<NormFactor> _normFactors = new List<NormFactor>();
        public virtual IReadOnlyCollection<NormFactor> NormFactors => _normFactors.AsReadOnly();

        // constructor
        public static ProductionProcess Create(string code, string name, Guid processGroupId, Guid unitOfMeasureId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }
            if (processGroupId == Guid.Empty)
            {
                throw new ArgumentException(CustomResponseMessage.ProcessGroupIdCannotBeEmpty);
            }
            if (unitOfMeasureId == Guid.Empty)
            {
                throw new ArgumentException(CustomResponseMessage.UnitOfMeasureIdCannotBeEmpty);
            }
            return new ProductionProcess
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId,
                UnitOfMeasureId = unitOfMeasureId
            };
        }

        public static ProductionProcess Create(Guid id, string code, string name, Guid processGroupId, Guid unitOfMeasureId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }
            if (processGroupId == Guid.Empty)
            {
                throw new ArgumentException(CustomResponseMessage.ProcessGroupIdCannotBeEmpty);
            }
            if (unitOfMeasureId == Guid.Empty)
            {
                throw new ArgumentException(CustomResponseMessage.UnitOfMeasureIdCannotBeEmpty);
            }
            return new ProductionProcess
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId,
                UnitOfMeasureId = unitOfMeasureId
            };
        }

        public void Update(string code, string name, Guid processGroupId, Guid unitOfMeasureId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }
            if (processGroupId == Guid.Empty)
            {
                throw new ArgumentException(CustomResponseMessage.ProcessGroupIdCannotBeEmpty);
            }
            if (unitOfMeasureId == Guid.Empty)
            {
                throw new ArgumentException(CustomResponseMessage.UnitOfMeasureIdCannotBeEmpty);
            }

            if (Code != null)
            {
                Code.Value = code.ToUpper();
            }

            Name = name;
            ProcessGroupId = processGroupId;
            UnitOfMeasureId = unitOfMeasureId;
        }

        public bool CheckChange(ProductionProcess dto)
        {
            return !(Code?.Value == dto.Code?.Value && Name == dto.Name && ProcessGroupId == dto.ProcessGroupId && UnitOfMeasureId == dto.UnitOfMeasureId);
        }
    }
}
