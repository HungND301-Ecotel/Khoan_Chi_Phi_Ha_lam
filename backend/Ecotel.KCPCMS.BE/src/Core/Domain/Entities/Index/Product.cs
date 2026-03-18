using Domain.Common.Contracts;
using Domain.Entities.Pricing;

namespace Domain.Entities.Index
{
    public class Product : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public Guid ProcessGroupId { get; protected set; }

        //Navigation Properties
        public virtual ProcessGroup? ProcessGroup { get; protected set; }
        public virtual Code? Code { get; protected set; }

        private IList<ProductUnitPrice> _productUnitPrices = new List<ProductUnitPrice>();
        public virtual IReadOnlyCollection<ProductUnitPrice> ProductUnitPrices => _productUnitPrices.AsReadOnly();
        //constructor
        public static Product Create(string code, string name, Guid processGroupId)
        {
            return new Product
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId,
            };
        }

        public static Product Create(Guid id, string code, string name, Guid processGroupId)
        {
            return new Product
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId,
            };
        }

        public void Update(string code, string name, Guid processGroupId)
        {
            Code.Value = code.ToUpper();
            Name = name;
            ProcessGroupId = processGroupId;
        }

        public bool CheckChange(Product dto)
        {
            return !(Code.Value == dto.Code.Value && Name == dto.Name && ProcessGroupId == dto.ProcessGroupId);
        }
    }
}
