using Domain.Common.Contracts;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing
{
    public class MaintainUnitPriceEquipment : AuditableEntity<Guid>
    {
        public Guid MaintainUnitPriceId { get; protected set; }
        public Guid PartId { get; protected set; }
        public double Quantity { get; protected set; }
        public decimal AverageMonthlyTunnelProduction { get; protected set; }

        private double? CachedTotal { get; set; }

        // Navigation properties
        public virtual MaintainUnitPrice? MaintainUnitPrice { get; protected set; } = null!;
        public virtual Part? Part { get; protected set; } = null!;

        //Constructor
        public static MaintainUnitPriceEquipment Create(Guid? maintainUnitPrice, Guid partId, double quantity, decimal averageMonthlyTunnelProduction)
        {
            if (quantity < 0)
            {
                throw new ArgumentException(CustomResponseMessage.QuantityCannotBeNegative);
            }

            if (averageMonthlyTunnelProduction < 0)
            {
                throw new ArgumentException(CustomResponseMessage.AverageMonthlyTunnelProductionCannotBeNegative);
            }

            return new MaintainUnitPriceEquipment
            {
                MaintainUnitPriceId = maintainUnitPrice ?? Guid.Empty,
                PartId = partId,
                Quantity = quantity,
                AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction,
            };
        }

        public double GetMaterialRate()
        {
            return Quantity / (double)(Part?.ReplacementTimeStandard ?? 1 * AverageMonthlyTunnelProduction);
        }

        public double GetMaterialCostPerMetres(DateOnly effectiveMonth)
        {
            if (CachedTotal.HasValue)
            {
                return CachedTotal.Value;
            }

            var curCost = Part?.Costs.FirstOrDefault(c =>
                c.StartMonth <= effectiveMonth && c.EndMonth >= effectiveMonth)?.Amount ?? 0;

            CachedTotal = curCost * GetMaterialRate();
            return CachedTotal.Value;
        }


        public void Update(Guid equipmentId, Guid partId, double quantity, decimal averageMonthlyTunnelProduction)
        {
            if (quantity < 0)
            {
                throw new ArgumentException(CustomResponseMessage.QuantityCannotBeNegative);
            }

            if (averageMonthlyTunnelProduction < 0)
            {
                throw new ArgumentException(CustomResponseMessage.AverageMonthlyTunnelProductionCannotBeNegative);
            }

            MaintainUnitPriceId = equipmentId;
            PartId = partId;
            Quantity = quantity;
            AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction;
        }
    }
}
