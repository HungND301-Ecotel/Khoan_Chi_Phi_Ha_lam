using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using Domain.Entities.Production;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class Part : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public Guid? UnitOfMeasureId { get; protected set; }
        public PartType Type { get; protected set; } = PartType.Part;
        public decimal ReplacementTimeStandard { get; protected set; }

        // Navigation properties
        public virtual UnitOfMeasure? UnitOfMeasure { get; protected set; }
        public virtual Code? Code { get; protected set; }

        private IList<EquipmentPart> _equipmentParts = new List<EquipmentPart>();
        public virtual IReadOnlyCollection<EquipmentPart> EquipmentParts => _equipmentParts.AsReadOnly();

        private IList<Cost> _costs = new List<Cost>();
        public virtual IReadOnlyCollection<Cost> Costs => _costs.AsReadOnly();

        private IList<MaintainUnitPriceEquipment> _maintainUnitPriceEquipments = new List<MaintainUnitPriceEquipment>();
        public virtual IReadOnlyCollection<MaintainUnitPriceEquipment> MaintainUnitPriceEquipments => _maintainUnitPriceEquipments.AsReadOnly();
        private IList<AcceptanceReportItem> _acceptanceReportItems = new List<AcceptanceReportItem>();
        public virtual IReadOnlyCollection<AcceptanceReportItem> AcceptanceReportItems => _acceptanceReportItems.AsReadOnly();
        private IList<PartProcessGroup> _partProcessGroups = new List<PartProcessGroup>();
        public virtual IReadOnlyCollection<PartProcessGroup> PartProcessGroups => _partProcessGroups.AsReadOnly();

        // constructor
        public static Part Create(string code, string name, Guid? unitOfMeasureId, decimal replacementTimeStandard, PartType type = PartType.Part)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            var part = new Part
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                UnitOfMeasureId = unitOfMeasureId,
                Type = type,
                ReplacementTimeStandard = replacementTimeStandard
            };

            return part;
        }

        public static Part Create(Guid id, string code, string name, Guid? unitOfMeasureId, decimal replacementTimeStandard, PartType type = PartType.Part)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            var part = new Part
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                UnitOfMeasureId = unitOfMeasureId,
                Type = type,
                ReplacementTimeStandard = replacementTimeStandard
            };

            return part;
        }


        public static Part Create(string code, string name, Guid? unitOfMeasureId, decimal replacementTimeStandard, IList<Equipment> equipments, PartType type = PartType.Part)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            ArgumentNullException.ThrowIfNull(equipments);
            if (!equipments.Any())
            {
                throw new ArgumentException(CustomResponseMessage.EquipmentNotFound);
            }

            var part = new Part
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                UnitOfMeasureId = unitOfMeasureId,
                Type = type,
                ReplacementTimeStandard = replacementTimeStandard
            };
            part.ReplaceEquipments(equipments);

            return part;
        }

        public static Part Create(Guid id, string code, string name, Guid? unitOfMeasureId, decimal replacementTimeStandard, IList<Equipment> equipments, PartType type = PartType.Part)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            ArgumentNullException.ThrowIfNull(equipments);
            if (!equipments.Any())
            {
                throw new ArgumentException(CustomResponseMessage.EquipmentNotFound);
            }

            var part = new Part
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                UnitOfMeasureId = unitOfMeasureId,
                Type = type,
                ReplacementTimeStandard = replacementTimeStandard
            };
            part.ReplaceEquipments(equipments);

            return part;
        }

        public void Update(string code, string name, Guid? unitOfMeasureId, decimal replacementTimeStandard, IList<Equipment> equipments, PartType type = PartType.Part)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }
            ArgumentNullException.ThrowIfNull(equipments);
            if (!equipments.Any())
            {
                throw new ArgumentException(CustomResponseMessage.EquipmentNotFound);
            }

            if (Code != null)
            {
                Code.Value = code.ToUpper();
            }

            Name = name;
            UnitOfMeasureId = unitOfMeasureId;
            Type = type;
            ReplacementTimeStandard = replacementTimeStandard;
            ReplaceEquipments(equipments);
        }

        public void Update(string code, string name, Guid? unitOfMeasureId, decimal replacementTimeStandard, PartType type = PartType.Part)
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
            ReplacementTimeStandard = replacementTimeStandard;
            Type = type;
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
            var currentEquipmentIds = EquipmentParts.Select(e => e.EquipmentId).OrderBy(id => id).ToList();
            var nextEquipmentIds = dto.EquipmentParts.Select(e => e.EquipmentId).OrderBy(id => id).ToList();

            return !(Code?.Value == dto.Code?.Value
                && Name == dto.Name
                && UnitOfMeasureId == dto.UnitOfMeasureId
                && currentEquipmentIds.SequenceEqual(nextEquipmentIds));
        }

        private void ReplaceEquipments(IList<Equipment> equipments)
        {
            _equipmentParts.Clear();
            foreach (var equipment in equipments.DistinctBy(e => e.Id))
            {
                _equipmentParts.Add(EquipmentPart.Create(equipment, this));
            }
        }
    }
}
