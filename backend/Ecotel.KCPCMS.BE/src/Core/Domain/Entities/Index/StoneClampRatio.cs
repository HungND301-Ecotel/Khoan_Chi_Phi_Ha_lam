using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class StoneClampRatio : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Value { get; protected set; }
        public double CoefficientValue { get; protected set; }
        public Guid HardnessId { get; protected set; }
        public Guid ProcessId { get; protected set; }

        //navigation properties
        public virtual Hardness? Hardness { get; protected set; }
        public virtual ProductionProcess? ProductionProcess { get; protected set; }

        private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
        public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();

        //controller
        public static StoneClampRatio Create(string value, double coefficientValue, Guid hardnessId, Guid processId)
        {
            if (coefficientValue <= 0)
            {
                throw new ArgumentException(CustomResponseMessage.CoefficientValueCannotBeZeroOrNegative);
            }

            return new StoneClampRatio
            {
                HardnessId = hardnessId,
                ProcessId = processId,
                Value = value,
                CoefficientValue = coefficientValue,
            };
        }

        public static StoneClampRatio Create(Guid id, string value, double coefficientValue, Guid hardnessId, Guid processId)
        {
            if (coefficientValue <= 0)
            {
                throw new ArgumentException(CustomResponseMessage.CoefficientValueCannotBeZeroOrNegative);
            }

            return new StoneClampRatio
            {
                Id = id,
                HardnessId = hardnessId,
                ProcessId = processId,
                Value = value,
                CoefficientValue = coefficientValue,
            };
        }

        public void Update(string value, double coefficientValue, Guid hardnessId, Guid processId)
        {
            if (coefficientValue <= 0)
            {
                throw new ArgumentException(CustomResponseMessage.CoefficientValueCannotBeZeroOrNegative);
            }

            Value = value;
            CoefficientValue = coefficientValue;
            HardnessId = hardnessId;
            ProcessId = processId;
        }

        public bool CheckChange(StoneClampRatio entity)
        {
            return !(Value == entity.Value && CoefficientValue == entity.CoefficientValue && HardnessId == entity.HardnessId && ProcessId == entity.ProcessId);
        }
    }
}
