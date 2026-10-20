using Domain.Common.Contracts;
using Domain.Entities.Pricing;

namespace Domain.Entities.Index
{
    public class StoneClampRatio : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Value { get; protected set; }
        public string? Note { get; protected set; }

        //navigation properties

        private IList<NormFactor> _normFactors = new List<NormFactor>();
        public virtual IReadOnlyCollection<NormFactor> NormFactors => _normFactors.AsReadOnly();

        private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
        public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();
        //controller
        public static StoneClampRatio Create(string value, string? note = null)
        {

            return new StoneClampRatio
            {
                Value = value,
                Note = NormalizeNote(note),
            };
        }

        public static StoneClampRatio Create(Guid id, string value, string? note = null)
        {
            return new StoneClampRatio
            {
                Id = id,
                Value = value,
                Note = NormalizeNote(note),
            };
        }

        public void Update(string value, string? note = null)
        {

            Value = value;
            Note = NormalizeNote(note);
        }

        public bool CheckChange(StoneClampRatio entity)
        {
            return !(Value == entity.Value && Note == NormalizeNote(entity.Note));
        }

        private static string? NormalizeNote(string? note)
        {
            return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }
    }
}
