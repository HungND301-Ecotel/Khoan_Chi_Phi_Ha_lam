using Domain.Common.Contracts;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing
{
    public class SlideUnitPriceAssignmentCode : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid SlideUnitPriceId { get; protected set; }
        public Guid AssignmentCodeId { get; protected set; }
        public Guid MaterialId { get; protected set; }
        public double Amount { get; protected set; }

        //Navigation Properties
        public virtual SlideUnitPrice? SlideUnitPrice { get; protected set; }
        public virtual AssignmentCode? AssignmentCode { get; protected set; }
        public virtual Material? Material { get; protected set; }

        private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
        public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();


        //Constructor
        public static SlideUnitPriceAssignmentCode Create(Guid assignmentCodeId, Guid materialId, double amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException(CustomResponseMessage.AmountCannotBeNegative);
            }

            return new SlideUnitPriceAssignmentCode
            {
                AssignmentCodeId = assignmentCodeId,
                MaterialId = materialId,
                Amount = amount
            };
        }

        public void Update(Guid assignmentCodeId, Guid materialId, double amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException(CustomResponseMessage.AmountCannotBeNegative);
            }

            AssignmentCodeId = assignmentCodeId;
            MaterialId = materialId;
            Amount = amount;
        }
    }
}
