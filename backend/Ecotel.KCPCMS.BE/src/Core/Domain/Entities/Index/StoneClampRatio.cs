using Domain.Common.Contracts;

namespace Domain.Entities.Index
{
    public class StoneClampRatio : AuditableEntity<Guid>, IAggregateRoot
    {
        public string Value { get; protected set; }

        //navigation properties

        private IList<NormFactor> _normFactors = new List<NormFactor>();
        public virtual IReadOnlyCollection<NormFactor> NormFactors => _normFactors.AsReadOnly();
        //controller
        public static StoneClampRatio Create(string value)
        {

            return new StoneClampRatio
            {
                Value = value,
            };
        }

        public static StoneClampRatio Create(Guid id, string value)
        {
            return new StoneClampRatio
            {
                Id = id,
                Value = value,
            };
        }

        public void Update(string value)
        {

            Value = value;
        }

        public bool CheckChange(StoneClampRatio entity)
        {
            return !(Value == entity.Value);
        }
    }
}
