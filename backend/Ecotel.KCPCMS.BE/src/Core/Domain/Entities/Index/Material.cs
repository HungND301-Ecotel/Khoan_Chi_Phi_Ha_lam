using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using Domain.Entities.Production;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class Material : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public Guid? AssigmentCodeId { get; protected set; }
        public Guid? UnitOfMeasureId { get; protected set; }
        public decimal UsageTime { get; protected set; }
        public MaterialType MaterialType { get; set; } = MaterialType.MaterialInContract;

        // Navigation properties
        public virtual AssignmentCode? AssignmentCode { get; protected set; }
        public virtual UnitOfMeasure? UnitOfMeasure { get; protected set; }
        public virtual Code? Code { get; protected set; }

        private IList<Cost> _costs = new List<Cost>();
        public virtual IReadOnlyCollection<Cost> Costs => _costs.AsReadOnly();

        private IList<AcceptanceReportItem> _acceptanceReportItems = new List<AcceptanceReportItem>();
        public virtual IReadOnlyCollection<AcceptanceReportItem> AcceptanceReportItems => _acceptanceReportItems.AsReadOnly();

        private IList<SlideUnitPriceAssignmentCode> _slideUnitPriceAssignmentCodes = new List<SlideUnitPriceAssignmentCode>();
        public virtual IReadOnlyCollection<SlideUnitPriceAssignmentCode> SlideUnitPriceAssignmentCodes => _slideUnitPriceAssignmentCodes.AsReadOnly();
        private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
        public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();
        //constructor
        public static Material Create(string code, string name, Guid? unitOfMeasureId, Guid? assigmentCodeId, decimal usageTime, MaterialType materialType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            //if (usageTime <= 0)
            //{
            //    throw new ArgumentException(CustomResponseMessage.UsageTimeCannotBeNegative);
            //}

            return new Material
            {
                Name = name,
                Code = new Code(code.ToUpper()),
                UnitOfMeasureId = unitOfMeasureId,
                AssigmentCodeId = assigmentCodeId,
                UsageTime = usageTime,
                MaterialType = materialType
            };
        }

        public static Material Create(Guid id, string code, string name, Guid? unitOfMeasureId, Guid? assigmentCodeId, decimal usageTime, MaterialType materialType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            //if (usageTime <= 0)
            //{
            //    throw new ArgumentException(CustomResponseMessage.UsageTimeCannotBeNegative);
            //}

            return new Material
            {
                Id = id,
                Name = name,
                Code = new Code(code.ToUpper()),
                UnitOfMeasureId = unitOfMeasureId,
                AssigmentCodeId = assigmentCodeId,
                UsageTime = usageTime,
                MaterialType = materialType
            };
        }

        public void Update(string code, string name, Guid? unitOfMeasureId, Guid? assigmentCodeId, decimal usageTime, MaterialType materialType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            if (usageTime <= 0)
            {
                throw new ArgumentException(CustomResponseMessage.UsageTimeCannotBeNegative);
            }

            Name = name;
            if (Code != null)
            {
                Code.Value = code.ToUpper();
            }

            UsageTime = usageTime;
            UnitOfMeasureId = unitOfMeasureId;
            AssigmentCodeId = assigmentCodeId;
            MaterialType = materialType;
        }

        public void AddMaterialCost(Cost materialCost)
        {
            ArgumentNullException.ThrowIfNull(materialCost);
            _costs.Add(materialCost);
        }

        public void AddMaterialCost(IList<Cost> materialCosts)
        {
            if (!materialCosts.Any())
            {
                throw new ArgumentException(CustomResponseMessage.CostsCannotBeEmpty);
            }

            foreach (var cost in materialCosts)
            {
                _costs.Add(cost);
            }
        }

        public void ClearCost()
        {
            _costs.Clear();
        }

        public bool CheckChange(Material dto)
        {
            return !(Code?.Value == dto.Code?.Value && Name == dto.Name && AssigmentCodeId == dto.AssigmentCodeId && UnitOfMeasureId == dto.UnitOfMeasureId && MaterialType == dto.MaterialType);
        }
    }
}
