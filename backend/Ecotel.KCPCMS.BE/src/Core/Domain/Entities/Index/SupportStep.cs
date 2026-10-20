using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class SupportStep : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Value { get; protected set; }
        public string? Note { get; protected set; }

        //Navigation Properties
        private IList<TunnelExcavationMaterialUnitPrice> _materialUnitPrices = new List<TunnelExcavationMaterialUnitPrice>();
        public virtual IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

        public static SupportStep Create(string value, string? note = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(CustomResponseMessage.SupportStepValueNullOrEmpty);
            }

            return new SupportStep
            {
                Value = value,
                Note = NormalizeNote(note)
            };
        }

        public void Update(string value, string? note = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(CustomResponseMessage.SupportStepValueNullOrEmpty);
            }
            Value = value;
            Note = NormalizeNote(note);
        }

        public bool CheckChange(SupportStep dto)
        {
            return !(Value == dto.Value
                && Note == NormalizeNote(dto.Note));
        }

        private static string? NormalizeNote(string? note)
        {
            return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }
    }
}
