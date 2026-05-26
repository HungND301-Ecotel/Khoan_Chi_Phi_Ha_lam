using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class Product : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public Guid ProcessGroupId { get; protected set; }
        public DateOnly StartMonth { get; protected set; }
        public DateOnly EndMonth { get; protected set; }

        //Navigation Properties
        public virtual ProcessGroup? ProcessGroup { get; protected set; }
        public virtual Code? Code { get; protected set; }

        private IList<ProductUnitPrice> _productUnitPrices = new List<ProductUnitPrice>();
        public virtual IReadOnlyCollection<ProductUnitPrice> ProductUnitPrices => _productUnitPrices.AsReadOnly();
        //constructor
        public static Product Create(
            string code,
            string name,
            Guid processGroupId,
            DateOnly startMonth,
            DateOnly endMonth)
        {
            Validate(code, name, startMonth, endMonth);

            return new Product
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId,
                StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
                EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            };
        }

        public static Product Create(
            Guid id,
            string code,
            string name,
            Guid processGroupId,
            DateOnly startMonth,
            DateOnly endMonth)
        {
            Validate(code, name, startMonth, endMonth);

            return new Product
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId,
                StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
                EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            };
        }

        public void Update(
            string code,
            string name,
            Guid processGroupId,
            DateOnly startMonth,
            DateOnly endMonth)
        {
            Validate(code, name, startMonth, endMonth);

            Code.Value = code.ToUpper();
            Name = name;
            ProcessGroupId = processGroupId;
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
        }

        public bool CheckChange(Product dto)
        {
            return !(Code.Value == dto.Code.Value
                && Name == dto.Name
                && ProcessGroupId == dto.ProcessGroupId
                && StartMonth == dto.StartMonth
                && EndMonth == dto.EndMonth);
        }

        private static void Validate(
            string code,
            string name,
            DateOnly startMonth,
            DateOnly endMonth)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            if (startMonth > endMonth)
            {
                throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
            }
        }
    }
}
