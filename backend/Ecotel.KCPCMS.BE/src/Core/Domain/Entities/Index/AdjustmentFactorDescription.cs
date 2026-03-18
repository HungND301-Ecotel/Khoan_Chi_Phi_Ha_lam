using Domain.Common.Contracts;
using Shared.Constants;

namespace Domain.Entities.Index;

public class AdjustmentFactorDescription : AuditableEntity<Guid>, IAggregateRoot
{
    public string Description { get; protected set; } = "";
    public Guid AdjustmentFactorId { get; protected set; }
    public double? MaintenanceAdjustmentValue { get; protected set; }
    public double? ElectricityAdjustmentValue { get; protected set; }

    // Navigation properties
    public AdjustmentFactor AdjustmentFactor { get; protected set; } = null!;

    private IList<PlannedMaintainCostAdjustmentFactorDescription> _plannedMaintainCostAdjustmentFactorDescriptions = new List<PlannedMaintainCostAdjustmentFactorDescription>();
    public virtual IReadOnlyCollection<PlannedMaintainCostAdjustmentFactorDescription> PlannedMaintainCostAdjustmentFactorDescriptions => _plannedMaintainCostAdjustmentFactorDescriptions.AsReadOnly();

    private IList<PlannedElectricityCostAdjustmentFactorDescription> _plannedElectricityCostAdjustmentFactorDescriptions = new List<PlannedElectricityCostAdjustmentFactorDescription>();
    public virtual IReadOnlyCollection<PlannedElectricityCostAdjustmentFactorDescription> PlannedElectricityCostAdjustmentFactorDescriptions => _plannedElectricityCostAdjustmentFactorDescriptions.AsReadOnly();

    public static AdjustmentFactorDescription Create(
        string description,
        Guid adjustmentFactorId,
        double? maintenanceAdjustmentValue,
        double? electricityAdjustmentValue)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(CustomResponseMessage.DescriptionCannotBeNullOrEmpty);
        }

        return new AdjustmentFactorDescription
        {
            Description = description,
            AdjustmentFactorId = adjustmentFactorId,
            MaintenanceAdjustmentValue = maintenanceAdjustmentValue,
            ElectricityAdjustmentValue = electricityAdjustmentValue
        };
    }

    public static AdjustmentFactorDescription Create(
        Guid id,
        string description,
        Guid adjustmentFactorId,
        double? maintenanceAdjustmentValue,
        double? electricityAdjustmentValue)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(CustomResponseMessage.DescriptionCannotBeNullOrEmpty);
        }

        return new AdjustmentFactorDescription
        {
            Id = id,
            Description = description,
            AdjustmentFactorId = adjustmentFactorId,
            MaintenanceAdjustmentValue = maintenanceAdjustmentValue,
            ElectricityAdjustmentValue = electricityAdjustmentValue
        };
    }

    public void Update(
        string description,
        Guid adjustmentFactorId,
        double? maintenanceAdjustmentValue,
        double? electricityAdjustmentValue)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(CustomResponseMessage.DescriptionCannotBeNullOrEmpty);
        }

        Description = description;
        AdjustmentFactorId = adjustmentFactorId;
        MaintenanceAdjustmentValue = maintenanceAdjustmentValue;
        ElectricityAdjustmentValue = electricityAdjustmentValue;
    }

    public bool CheckChange(AdjustmentFactorDescription dto)
    {
        return !(Description == dto.Description && AdjustmentFactorId == dto.AdjustmentFactorId && MaintenanceAdjustmentValue == dto.MaintenanceAdjustmentValue && ElectricityAdjustmentValue == dto.ElectricityAdjustmentValue);
    }
}
