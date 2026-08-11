using System;
using System.Collections.Generic;
using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MechanizedTransportUnitPrice;

public sealed class ServiceAndCraneVehicleUnitPrice : MechanizedTransportUnitPrice
{
    public static ServiceAndCraneVehicleUnitPrice Create(
        Guid assignmentCodeId,
        string equipmentQuality,
        Guid productionProcessId,
        DateOnly startMonth,
        DateOnly endMonth,
        IEnumerable<MechanizedTransportUnitPriceDetailInput> details)
    {
        ValidateHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);

        var entity = new ServiceAndCraneVehicleUnitPrice();
 
        entity.SetHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
        entity.ReplaceDetails(details, false);

        return entity;
    }

    public void Update(
        Guid assignmentCodeId,
        string equipmentQuality,
        Guid productionProcessId,
        DateOnly startMonth,
        DateOnly endMonth,
        IEnumerable<MechanizedTransportUnitPriceDetailInput> details)
    {
        ValidateHeaderForUpdate(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);

        SetHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
        ReplaceDetails(details, false);
    }
}