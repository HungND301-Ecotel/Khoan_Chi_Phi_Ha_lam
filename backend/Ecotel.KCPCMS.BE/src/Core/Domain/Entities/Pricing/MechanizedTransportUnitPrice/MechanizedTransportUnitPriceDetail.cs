using System;
using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MechanizedTransportUnitPrice;

public record MechanizedTransportUnitPriceDetailInput(Guid? HaulDistanceId, decimal FuelUnitPrice, decimal? PowerUnitPrice, decimal MaintenanceUnitPrice);

public class MechanizedTransportUnitPriceDetail : AuditableEntity<Guid>
{
    public Guid MechanizedTransportUnitPriceId { get; protected set; }
    public Guid? HaulDistanceId { get; protected set; }
    public decimal FuelUnitPrice { get; protected set; }
    public decimal? PowerUnitPrice { get; protected set; }
    public decimal MaintenanceUnitPrice { get; protected set; }

    public virtual MechanizedTransportUnitPrice? MechanizedTransportUnitPrice { get; protected set; }
    public virtual HaulDistance? HaulDistance { get; protected set; }

    internal static MechanizedTransportUnitPriceDetail Create(Guid? haulDistanceId, decimal fuelUnitPrice, decimal? powerUnitPrice, decimal maintenanceUnitPrice)
    {
        if (fuelUnitPrice < 0 || maintenanceUnitPrice < 0 || powerUnitPrice < 0)
        {
            throw new ArgumentException("Đơn giá không được là số âm.");
        }

        return new MechanizedTransportUnitPriceDetail
        {
            HaulDistanceId = haulDistanceId,
            FuelUnitPrice = fuelUnitPrice,
            PowerUnitPrice = powerUnitPrice,
            MaintenanceUnitPrice = maintenanceUnitPrice
        };
    }
}