using Domain.Common.Contracts;
using Domain.Common.Enums;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class AdjustmentFactor : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public Guid? FixedKeyId { get; protected set; }
        public Guid ProcessGroupId { get; protected set; }

        // Navigation properties
        public virtual ProcessGroup? ProcessGroup { get; protected set; }
        public virtual Code? Code { get; protected set; }
        public virtual FixedKey? FixedKey { get; protected set; }

        private IList<AdjustmentFactorDescription> _adjustmentFactorDescriptions = new List<AdjustmentFactorDescription>();
        public IReadOnlyList<AdjustmentFactorDescription> AdjustmentFactorDescriptions => _adjustmentFactorDescriptions.ToList();

        public static AdjustmentFactor Create(FixedKey fixedKey, string name, Guid processGroupId)
        {
            ArgumentNullException.ThrowIfNull(fixedKey);

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new AdjustmentFactor
            {
                Code = new Code(fixedKey.Key),
                FixedKeyId = fixedKey.Id,
                Name = name,
                ProcessGroupId = processGroupId,
            };
        }

        public void Update(FixedKey fixedKey, string name, Guid processGroupId)
        {
            ArgumentNullException.ThrowIfNull(fixedKey);

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            if (Code != null)
            {
                Code.Value = fixedKey.Key;
            }
            else
            {
                Code = new Code(fixedKey.Key);
            }

            FixedKeyId = fixedKey.Id;
            Name = name;
            ProcessGroupId = processGroupId;
        }

        public void UpdateProcessGroupInfo(Guid processGroupId, string processGroupName)
        {
            ProcessGroupId = processGroupId;
        }

        public bool CheckChange(AdjustmentFactor dto)
        {
            return !(Code?.Value == dto.Code?.Value && Name == dto.Name && ProcessGroupId == dto.ProcessGroupId && FixedKeyId == dto.FixedKeyId);
        }
    }
}
