using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class TransportRoute : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid CodeId { get; protected set; }
        public string Name { get; protected set; }
        public string? Note { get; protected set; }
        public Guid ProductionProcessId { get; protected set; }
        public bool IsSpecialLowVolume { get; protected set; }

        public virtual Code? Code { get; protected set; }
        public virtual ProductionProcess? ProductionProcess { get; protected set; }

        private IList<TransportUnitPrice> _transportUnitPrices = new List<TransportUnitPrice>();
        public virtual IReadOnlyCollection<TransportUnitPrice> TransportUnitPrices => _transportUnitPrices.AsReadOnly();

        public static TransportRoute Create(
            string code,
            string name,
            string? note,
            Guid productionProcessId,
            bool isSpecialLowVolume)
        {
            Validate(code, name, productionProcessId);

            return new TransportRoute
            {
                Code = new Code(code.ToUpper()),
                Name = name,
                Note = note,
                ProductionProcessId = productionProcessId,
                IsSpecialLowVolume = isSpecialLowVolume,
            };
        }

        public static TransportRoute Create(
            Guid id,
            string code,
            string name,
            string? note,
            Guid productionProcessId,
            bool isSpecialLowVolume)
        {
            Validate(code, name, productionProcessId);

            return new TransportRoute
            {
                Id = id,
                Code = new Code(code.ToUpper()),
                Name = name,
                Note = note,
                ProductionProcessId = productionProcessId,
                IsSpecialLowVolume = isSpecialLowVolume,
            };
        }

        public void Update(
            string code,
            string name,
            string? note,
            Guid productionProcessId,
            bool isSpecialLowVolume)
        {
            Validate(code, name, productionProcessId);

            if (Code != null)
            {
                Code.Value = code.ToUpper();
            }

            Name = name;
            Note = note;
            ProductionProcessId = productionProcessId;
            IsSpecialLowVolume = isSpecialLowVolume;
        }

        public bool CheckChange(TransportRoute dto)
        {
            return !(Code?.Value == dto.Code?.Value
                && Name == dto.Name
                && Note == dto.Note
                && ProductionProcessId == dto.ProductionProcessId
                && IsSpecialLowVolume == dto.IsSpecialLowVolume);
        }

        private static void Validate(string code, string name, Guid productionProcessId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            if (productionProcessId == Guid.Empty)
            {
                throw new ArgumentException(CustomResponseMessage.ProductionProcessIdCannotBeEmpty);
            }
        }
    }
}