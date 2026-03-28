using Domain.Common.Contracts;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Production;

public class ActualEletricityEquipment : AuditableEntity<Guid>
{
    public Guid ActualElectricityCostId { get; protected set; }
    public Guid EquipmentId { get; protected set; }

    public double ActualElectricityConsumption { get; protected set; }

    public ActualElectricityCost? ActualElectricityCost { get; protected set; }
    public Equipment? Equipment { get; protected set; }

    public static ActualEletricityEquipment Create(Guid actualElectricityCostId, Guid equipmentId, double actualElectricityConsumption)
    {
        if (actualElectricityConsumption < 0)
        {
            throw new ArgumentException(CustomResponseMessage.QuantityCannotBeNegative);
        }

        return new ActualEletricityEquipment
        {
            ActualElectricityCostId = actualElectricityCostId,
            EquipmentId = equipmentId,
            ActualElectricityConsumption = actualElectricityConsumption
        };
    }

    public void Update(Guid equipmentId, double actualElectricityConsumption)
    {
        if (actualElectricityConsumption < 0)
        {
            throw new ArgumentException(CustomResponseMessage.QuantityCannotBeNegative);
        }

        EquipmentId = equipmentId;
        ActualElectricityConsumption = actualElectricityConsumption;
    }

    public double GetCurrentElectricityCost(DateOnly effectiveDate)
    {
        if (Equipment != null)
        {
            return ActualElectricityConsumption * Equipment.GetEffectiveDateCost(effectiveDate);
        }

        return 0;
    }
}
