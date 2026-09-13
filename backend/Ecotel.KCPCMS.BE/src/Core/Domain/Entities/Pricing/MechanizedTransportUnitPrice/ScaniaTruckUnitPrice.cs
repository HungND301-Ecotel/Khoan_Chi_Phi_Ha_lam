using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MechanizedTransportUnitPrice;

public class ScaniaTruckUnitPrice : MechanizedTransportUnitPrice
{
    public Guid CargoTypeId { get; protected set; }
    public Guid? DumpingLocationId { get; protected set; }

    public virtual CargoType? CargoType { get; protected set; }
    public virtual TransportLocation? DumpingLocation { get; protected set; }

    private readonly IList<ScaniaTruckUnitPriceReceivingLocation> _receivingLocations = new List<ScaniaTruckUnitPriceReceivingLocation>();
    public virtual IReadOnlyCollection<ScaniaTruckUnitPriceReceivingLocation> ReceivingLocations => _receivingLocations.AsReadOnly();

    public static ScaniaTruckUnitPrice Create(
        Guid assignmentCodeId,
        string equipmentQuality,
        Guid productionProcessId,
        Guid cargoTypeId,
        IEnumerable<Guid>? receivingLocationIds,
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
            DumpingLocationId = dumpingLocationId
        };
        entity.SetHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
        entity.ReplaceDetails(details, true);
        entity.ReplaceReceivingLocations(receivingLocationIds);

        return entity;
    }

    public void Update(
        Guid assignmentCodeId,
        string equipmentQuality,
        Guid productionProcessId,
        Guid cargoTypeId,
        IEnumerable<Guid>? receivingLocationIds,
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
        DumpingLocationId = dumpingLocationId;
        SetHeader(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
        ReplaceDetails(details, true);
        ReplaceReceivingLocations(receivingLocationIds);
    }

    public void ReplaceReceivingLocations(IEnumerable<Guid>? locationIds)
    {
        _receivingLocations.Clear();
        if (locationIds != null)
        {
            foreach (var locId in locationIds.Distinct())
            {
                if (locId != Guid.Empty)
                {
                    _receivingLocations.Add(ScaniaTruckUnitPriceReceivingLocation.Create(locId));
                }
            }
        }
    }
}