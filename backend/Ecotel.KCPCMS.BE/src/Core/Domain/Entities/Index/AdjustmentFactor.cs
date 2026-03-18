using Domain.Common.Contracts;
using Domain.Common.Enums;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class AdjustmentFactor : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public AdjustmentFactorType Type { get; protected set; } = AdjustmentFactorType.None;
        public Guid ProcessGroupId { get; protected set; }

        // Navigation properties
        public virtual ProcessGroup? ProcessGroup { get; protected set; }
        public virtual Code? Code { get; protected set; }

        private IList<AdjustmentFactorDescription> _adjustmentFactorDescriptions = new List<AdjustmentFactorDescription>();
        public IReadOnlyList<AdjustmentFactorDescription> AdjustmentFactorDescriptions => _adjustmentFactorDescriptions.ToList();

        // Constructor
        public static AdjustmentFactor Create(string code, string name, Guid processGroupId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new AdjustmentFactor
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId,
            };
        }

        public static AdjustmentFactor Create(Guid id, string code, string name, Guid processGroupId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new AdjustmentFactor
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                ProcessGroupId = processGroupId,
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

            if (Code != null)
            {
                Code.Value = code.ToUpper();
            }

            Name = name;
            ProcessGroupId = processGroupId;
        }

        public void UpdateProcessGroupInfo(Guid processGroupId, string processGroupName)
        {
            ProcessGroupId = processGroupId;
        }

        public bool CheckChange(AdjustmentFactor dto)
        {
            return !(Code?.Value == dto.Code?.Value && Name == dto.Name && ProcessGroupId == ProcessGroupId);
        }
    }
}
