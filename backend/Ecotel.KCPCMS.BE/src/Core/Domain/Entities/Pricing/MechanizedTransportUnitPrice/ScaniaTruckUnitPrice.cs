using System;
using System.Collections.Generic;
using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MechanizedTransportUnitPrice;

public class ScaniaTruckUnitPrice : MechanizedTransportUnitPrice
{
    public Guid CargoTypeId { get; protected set; }
    public Guid? ReceivingLocationId { get; protected set; }
    public Guid? DumpingLocationId { get; protected set; }

    public virtual CargoType? CargoType { get; protected set; }
    public virtual TransportLocation? ReceivingLocation { get; protected set; }
    public virtual TransportLocation? DumpingLocation { get; protected set; }

    public static ScaniaTruckUnitPrice Create(
        Guid assignmentCodeId,
        string equipmentQuality,
        Guid productionProcessId,
        Guid cargoTypeId,
        Guid? receivingLocationId,
        Guid? dumpingLocationId,
        DateOnly startMonth,
        DateOnly endMonth,
        IEnumerable<MechanizedTransportUnitPriceDetailInput> details)
    {
        ValidateHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
        if (cargoTypeId == Guid.Empty)
        {
            throw new ArgumentException("Chủng loại hàng không được để trống.");
        }

        var entity = new ScaniaTruckUnitPrice
        {
            CargoTypeId = cargoTypeId,
            ReceivingLocationId = receivingLocationId,
            DumpingLocationId = dumpingLocationId
        };
        entity.SetHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
        entity.ReplaceDetails(details, true);

        return entity;
    }

    public void Update(
        Guid assignmentCodeId,
        string equipmentQuality,
        Guid productionProcessId,
        Guid cargoTypeId,
        Guid? receivingLocationId,
        Guid? dumpingLocationId,
        DateOnly startMonth,
        DateOnly endMonth,
        IEnumerable<MechanizedTransportUnitPriceDetailInput> details)
    {
        ValidateHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
        if (cargoTypeId == Guid.Empty)
        {
            throw new ArgumentException("Chủng loại hàng không được để trống.");
        }

        CargoTypeId = cargoTypeId;
        ReceivingLocationId = receivingLocationId;
        DumpingLocationId = dumpingLocationId;
        SetHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
        ReplaceDetails(details, true);
    }
}