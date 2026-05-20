using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class UnitOfMeasure : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Name { get; protected set; }

        // Navigation properties
        private IList<AssignmentCode> _assignmentCodes = new List<AssignmentCode>();
        public virtual IReadOnlyCollection<AssignmentCode> AssignmentCodes => _assignmentCodes.AsReadOnly();

        private IList<Material> _materials = new List<Material>();
        public virtual IReadOnlyCollection<Material> Materials => _materials.AsReadOnly();

        private IList<ProductUnitPrice> _productUnitPrices = new List<ProductUnitPrice>();
        public virtual IReadOnlyCollection<ProductUnitPrice> ProductUnitPrices => _productUnitPrices.AsReadOnly();


        //constructor
        public static UnitOfMeasure Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new UnitOfMeasure
            {
                Name = name
            };
        }

        public void Update(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            Name = name;
        }

        public bool CheckChange(UnitOfMeasure dto)
        {
            return !Name.Equals(dto.Name);
        }
    }
}
