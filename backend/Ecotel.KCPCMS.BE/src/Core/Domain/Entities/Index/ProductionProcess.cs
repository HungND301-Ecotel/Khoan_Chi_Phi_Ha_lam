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

        // Navigation properties
        public virtual Code? Code { get; protected set; }
        public virtual ProcessGroup? ProcessGroup { get; protected set; }

        private IList<StoneClampRatio> _stoneClampRatios = new List<StoneClampRatio>();
        public virtual IReadOnlyCollection<StoneClampRatio> StoneClampRatios => _stoneClampRatios.AsReadOnly();

        private IList<MaterialUnitPrice> _materialUnitPrices = new List<MaterialUnitPrice>();
        public virtual IReadOnlyCollection<MaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

        // constructor
        public static ProductionProcess Create(string code, string name, Guid processGroupId)
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
            return new ProductionProcess
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId
            };
        }

        public static ProductionProcess Create(Guid id, string code, string name, Guid processGroupId)
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
            return new ProductionProcess
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId
            };
        }

        public void Update(string code, string name, Guid processGroupId)
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

            if (Code != null)
            {
                Code.Value = code.ToUpper();
            }

            Name = name;
            ProcessGroupId = processGroupId;
        }

        public bool CheckChange(ProductionProcess dto)
        {
            return !(Code?.Value == dto.Code?.Value && Name == dto.Name && ProcessGroupId == dto.ProcessGroupId);
        }
    }
}
