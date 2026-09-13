using System;
using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MechanizedTransportUnitPrice;

public class ScaniaTruckUnitPriceReceivingLocation : AuditableEntity<Guid>
{
    public Guid ScaniaTruckUnitPriceId { get; protected set; }
    public Guid TransportLocationId { get; protected set; }

    public virtual ScaniaTruckUnitPrice? ScaniaTruckUnitPrice { get; protected set; }
    public virtual TransportLocation? TransportLocation { get; protected set; }

    internal static ScaniaTruckUnitPriceReceivingLocation Create(Guid transportLocationId)
    {
        if (transportLocationId == Guid.Empty)
        {
            throw new ArgumentException("Vị trí nhận không được để trống.");
        }

        return new ScaniaTruckUnitPriceReceivingLocation
        {
            TransportLocationId = transportLocationId
        };
    }
}
