using Domain.Common.Contracts;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing
{
    public class SlideUnitPrice : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public Guid ProcessGroupId { get; protected set; }
        public Guid PassportId { get; protected set; }
        public Guid HardnessId { get; protected set; }
        public DateOnly StartMonth { get; protected set; }
        public DateOnly EndMonth { get; protected set; }

        // Navigation properties
        public virtual Code? Code { get; protected set; }
        public virtual ProcessGroup? ProcessGroup { get; protected set; }
        public virtual Passport? Passport { get; protected set; }
        public virtual Hardness? Hardness { get; protected set; }

        private IList<SlideUnitPriceAssignmentCode> _slideUnitPriceAssignmentCodes = new List<SlideUnitPriceAssignmentCode>();
        public virtual IReadOnlyCollection<SlideUnitPriceAssignmentCode> SlideUnitPriceAssignmentCodes => _slideUnitPriceAssignmentCodes.AsReadOnly();

        //Constructor
        public static SlideUnitPrice Create(
            string code,
            Guid processGroupId,
            Guid hardnessId,
            Guid passportId,
            DateOnly startMonth,
            DateOnly endMonth,
            IList<SlideUnitPriceAssignmentCode> slideUnitPriceAssignmentCodes)
        {
            if (startMonth > endMonth)
            {
                throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
            }

            var slideUnitPrice = new SlideUnitPrice
            {
                Code = new Code(code.ToUpper()),
                ProcessGroupId = processGroupId,
                PassportId = passportId,
                HardnessId = hardnessId,
                StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
                EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1)
            };
            foreach (var item in slideUnitPriceAssignmentCodes)
            {
                slideUnitPrice._slideUnitPriceAssignmentCodes.Add(item);
            }
            return slideUnitPrice;
        }

        public double GetCurrentTotalPrice()
        {
            return SlideUnitPriceAssignmentCodes.Sum(m => m.Amount);
        }

        public void Update
        (
            string code,
            Guid processGroupId,
            Guid hardnessId,
            Guid passportId,
            DateOnly startMonth,
            DateOnly endMonth,
            IList<SlideUnitPriceAssignmentCode> slideUnitPriceAssignmentCodes)
        {
            if (startMonth > endMonth)
            {
                throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
            }

            if (Code != null)
            {
                Code.Value = code.ToUpper();
            }

            ProcessGroupId = processGroupId;
            PassportId = passportId;
            HardnessId = hardnessId;
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);

            _slideUnitPriceAssignmentCodes.Clear();

            foreach (var item in slideUnitPriceAssignmentCodes)
            {
                _slideUnitPriceAssignmentCodes.Add(item);
            }
        }
    }
}
