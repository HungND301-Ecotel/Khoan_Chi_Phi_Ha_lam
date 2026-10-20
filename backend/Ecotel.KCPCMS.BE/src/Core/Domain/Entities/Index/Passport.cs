using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index
{
    public class Passport : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Name { get; protected set; }
        public string Sd { get; protected set; }
        public string Sc { get; protected set; }
        public string? Note { get; protected set; }

        //Navigation Properties
        private IList<TunnelExcavationMaterialUnitPrice> _materialUnitPrices = new List<TunnelExcavationMaterialUnitPrice>();
        public virtual IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();
        private IList<SlideUnitPrice> _slideUnitPrices = new List<SlideUnitPrice>();
        public virtual IReadOnlyCollection<SlideUnitPrice> SlideUnitPrices => _slideUnitPrices.AsReadOnly();

        // Constructor
        public static Passport Create(string name, string sd, string sc, string? note = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new Passport
            {
                Name = name,
                Sd = sd,
                Sc = sc,
                Note = NormalizeNote(note)
            };
        }

        public static Passport Create(Guid id, string name, string sd, string sc, string? note = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            return new Passport
            {
                Id = id,
                Name = name,
                Sd = sd,
                Sc = sc,
                Note = NormalizeNote(note)
            };
        }

        public void Update(string name, string sd, string sc, string? note = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
            }

            Name = name;
            Sd = sd;
            Sc = sc;
            Note = NormalizeNote(note);
        }

        public bool CheckChange(Passport dto)
        {
            return !(Name == dto.Name && Sd == dto.Sd && Sc == dto.Sc && Note == NormalizeNote(dto.Note));
        }

        public string GetFullname()
        {
            return $"H/c {Name}; {Sd}; {Sc}";
        }

        private static string? NormalizeNote(string? note)
        {
            return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }
    }
}
