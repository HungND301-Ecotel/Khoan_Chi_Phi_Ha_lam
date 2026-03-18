using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class Part : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public Guid? UnitOfMeasureId { get; protected set; }
        public Guid EquipmentId { get; protected set; }

        // Navigation properties
        public virtual UnitOfMeasure? UnitOfMeasure { get; protected set; }
        public virtual Equipment? Equipment { get; protected set; }
        public virtual Code? Code { get; protected set; }

        private IList<Cost> _costs = new List<Cost>();
        public virtual IReadOnlyCollection<Cost> Costs => _costs.AsReadOnly();

        private IList<MaintainUnitPriceEquipment> _maintainUnitPriceEquipments = new List<MaintainUnitPriceEquipment>();
        public virtual IReadOnlyCollection<MaintainUnitPriceEquipment> MaintainUnitPriceEquipments => _maintainUnitPriceEquipments.AsReadOnly();

        // constructor
        public static Part Create(string code, string name, Guid? unitOfMeasureId, Guid equipmentId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }
            return new Part
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                UnitOfMeasureId = unitOfMeasureId,
                EquipmentId = equipmentId
            };
        }

        public static Part Create(Guid id, string code, string name, Guid? unitOfMeasureId, Guid equipmentId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }
            return new Part
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                UnitOfMeasureId = unitOfMeasureId,
                EquipmentId = equipmentId
            };
        }

        public void Update(string code, string name, Guid? unitOfMeasureId, Guid equipmentId)
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
            UnitOfMeasureId = unitOfMeasureId;
            EquipmentId = equipmentId;
        }

        public void AddCost(Cost cost)
        {
            ArgumentNullException.ThrowIfNull(cost);
            _costs.Add(cost);
        }

        public void ClearCost()
        {
            _costs.Clear();
        }

        public void AddCost(IList<Cost> costs)
        {
            ArgumentNullException.ThrowIfNull(costs);
            foreach (var cost in costs)
            {
                _costs.Add(cost);
            }
        }

        public double GetPartCost(DateOnly effectiveMonth)
        {
            return Costs?
                .FirstOrDefault(c => c.StartMonth <= effectiveMonth && c.EndMonth >= effectiveMonth)
                ?.Amount ?? 0;
        }

        public bool CheckChange(Part dto)
        {
            return !(Code?.Value == dto.Code?.Value && Name == dto.Name && UnitOfMeasureId == dto.UnitOfMeasureId && EquipmentId == dto.EquipmentId);
        }
    }
}
