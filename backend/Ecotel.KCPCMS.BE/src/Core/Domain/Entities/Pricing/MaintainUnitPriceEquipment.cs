using Domain.Common.Contracts;
using Domain.Entities.Index;
using Domain.Entities.Production;
using Shared.Constants;

namespace Domain.Entities.Pricing
{
    public class MaintainUnitPriceEquipment : AuditableEntity<Guid>
    {
        public Guid MaintainUnitPriceId { get; protected set; }
        public Guid PartId { get; protected set; }
        public double Quantity { get; protected set; }
        public decimal ReplacementTimeStandard { get; protected set; }
        public decimal AverageMonthlyTunnelProduction { get; protected set; }

        private double? CachedTotal { get; set; }

        // Navigation properties
        public virtual MaintainUnitPrice? MaintainUnitPrice { get; protected set; } = null!;
        public virtual Part? Part { get; protected set; } = null!;

        private IList<AcceptanceReportItem> _acceptanceReportItems = new List<AcceptanceReportItem>();
        public virtual IReadOnlyCollection<AcceptanceReportItem> AcceptanceReportItems => _acceptanceReportItems.AsReadOnly();

        //Constructor
        public static MaintainUnitPriceEquipment Create(Guid? maintainUnitPrice, Guid partId, double quantity, decimal replacementTimeStandard, decimal averageMonthlyTunnelProduction)
        {
            if (quantity < 0)
            {
                throw new ArgumentException(CustomResponseMessage.QuantityCannotBeNegative);
            }

            if (replacementTimeStandard < 0)
            {
                throw new ArgumentException(CustomResponseMessage.ReplacementTimeStandardCannotBeNegative);
            }

            if (averageMonthlyTunnelProduction < 0)
            {
                throw new ArgumentException(CustomResponseMessage.AverageMonthlyTunnelProductionCannotBeNegative);
            }

            double materialRate = quantity / (double)(replacementTimeStandard * averageMonthlyTunnelProduction);
            return new MaintainUnitPriceEquipment
            {
                MaintainUnitPriceId = maintainUnitPrice ?? Guid.Empty,
                PartId = partId,
                Quantity = quantity,
                ReplacementTimeStandard = replacementTimeStandard,
                AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction,
            };
        }

        public double GetMaterialRate()
        {
            return Quantity / (double)(ReplacementTimeStandard * AverageMonthlyTunnelProduction);
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


        public void Update(Guid equipmentId, Guid partId, double quantity, decimal replacementTimeStandard, decimal averageMonthlyTunnelProduction)
        {
            if (quantity < 0)
            {
                throw new ArgumentException(CustomResponseMessage.QuantityCannotBeNegative);
            }

            if (replacementTimeStandard < 0)
            {
                throw new ArgumentException(CustomResponseMessage.ReplacementTimeStandardCannotBeNegative);
            }

            if (averageMonthlyTunnelProduction < 0)
            {
                throw new ArgumentException(CustomResponseMessage.AverageMonthlyTunnelProductionCannotBeNegative);
            }

            double materialRate = quantity / (double)(replacementTimeStandard * averageMonthlyTunnelProduction);

            MaintainUnitPriceId = equipmentId;
            PartId = partId;
            Quantity = quantity;
            ReplacementTimeStandard = replacementTimeStandard;
            AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction;
        }
    }
}
