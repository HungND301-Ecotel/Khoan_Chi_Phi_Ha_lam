using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Pricing;

public class PlannedTransportCost : AuditableEntity<Guid>
{
    public Guid TransportPlanLineId { get; protected set; }
    public Guid? TransportUnitPriceId { get; protected set; }
    public Guid? MechanizedTransportUnitPriceDetailId { get; protected set; }

    public LowValuePerishableSupplyInclusion LowValuePerishableSupplyInclusion { get; protected set; }
        = LowValuePerishableSupplyInclusion.Exclude;

    private double? CachedPlannedTotalPrice { get; set; }

    // Navigation properties
    public virtual TransportPlanLine? TransportPlanLine { get; protected set; }
    public virtual TransportUnitPrice? TransportUnitPrice { get; protected set; }
    public virtual MechanizedTransportUnitPriceDetail? MechanizedTransportUnitPriceDetail { get; protected set; }

    private IList<PlannedTransportCostAdjustmentFactor> _adjustmentFactors = new List<PlannedTransportCostAdjustmentFactor>();
    public virtual IReadOnlyCollection<PlannedTransportCostAdjustmentFactor> AdjustmentFactors => _adjustmentFactors.AsReadOnly();

    // Đơn giá gốc lấy qua FK tại thời điểm đọc, không lưu cứng 
    public decimal? MaterialFuelUnitPrice => TransportUnitPrice?.MaterialFuelUnitPrice
        ?? MechanizedTransportUnitPriceDetail?.FuelUnitPrice;
    public decimal? PowerUnitPrice => TransportUnitPrice?.PowerUnitPrice
        ?? MechanizedTransportUnitPriceDetail?.PowerUnitPrice;
    public decimal? MaintenanceUnitPrice => TransportUnitPrice?.MaintenanceUnitPrice
        ?? MechanizedTransportUnitPriceDetail?.MaintenanceUnitPrice;


    public double? PowerCoefficient => _adjustmentFactors.Any()
        ? _adjustmentFactors.Aggregate(1.0, (acc, x) => acc * x.PowerEffectiveValue)
        : null;
    public double? MaintenanceCoefficient => _adjustmentFactors.Any()
        ? _adjustmentFactors.Aggregate(1.0, (acc, x) => acc * x.MaintenanceEffectiveValue)
        : null;

    public bool IsLowVolumeCase => TransportUnitPrice?.IsLowVolumeCase ?? false;


    public double GetPlannedTotalPrice()
    {
        if (CachedPlannedTotalPrice.HasValue)
        {
            return CachedPlannedTotalPrice.Value;
        }

        var unitTotal = (double)(MaterialFuelUnitPrice ?? 0)
            + (double)(PowerUnitPrice ?? 0) * (PowerCoefficient ?? 1)
            + (double)(MaintenanceUnitPrice ?? 0) * (MaintenanceCoefficient ?? 1);

        CachedPlannedTotalPrice = IsLowVolumeCase
            ? unitTotal
            : (TransportPlanLine?.ProductionMeters ?? 0) * unitTotal;
        return CachedPlannedTotalPrice.Value;
    }

    public static PlannedTransportCost Create(
        Guid transportPlanLineId,
        Guid? transportUnitPriceId,
        Guid? mechanizedTransportUnitPriceDetailId,
        LowValuePerishableSupplyInclusion lowValuePerishableSupplyInclusion = LowValuePerishableSupplyInclusion.Exclude)
    {
        Validate(transportPlanLineId, transportUnitPriceId, mechanizedTransportUnitPriceDetailId);

        return new PlannedTransportCost
        {
            TransportPlanLineId = transportPlanLineId,
            TransportUnitPriceId = transportUnitPriceId,
            MechanizedTransportUnitPriceDetailId = mechanizedTransportUnitPriceDetailId,
            LowValuePerishableSupplyInclusion = lowValuePerishableSupplyInclusion,
        };
    }

    public void Update(
        Guid? transportUnitPriceId,
        Guid? mechanizedTransportUnitPriceDetailId,
        LowValuePerishableSupplyInclusion lowValuePerishableSupplyInclusion = LowValuePerishableSupplyInclusion.Exclude)
    {
        Validate(TransportPlanLineId, transportUnitPriceId, mechanizedTransportUnitPriceDetailId);

        TransportUnitPriceId = transportUnitPriceId;
        MechanizedTransportUnitPriceDetailId = mechanizedTransportUnitPriceDetailId;
        LowValuePerishableSupplyInclusion = lowValuePerishableSupplyInclusion;
    }

    public void AddAdjustmentFactors(IEnumerable<PlannedTransportCostAdjustmentFactor> factors)
    {
        foreach (var factor in factors)
        {
            _adjustmentFactors.Add(factor);
        }
    }

    public void ClearAdjustmentFactors()
    {
        _adjustmentFactors.Clear();
    }

    private static void Validate(
        Guid transportPlanLineId,
        Guid? transportUnitPriceId,
        Guid? mechanizedTransportUnitPriceDetailId)
    {
        if (transportPlanLineId == Guid.Empty)
        {
            throw new ArgumentException(CustomResponseMessage.TransportPlanLineNotFound);
        }

        var hasTransportUnitPrice = transportUnitPriceId.HasValue;
        var hasMechanizedTransportUnitPriceDetail = mechanizedTransportUnitPriceDetailId.HasValue;

        if (hasTransportUnitPrice == hasMechanizedTransportUnitPriceDetail)
        {
            throw new ArgumentException(CustomResponseMessage.PlannedTransportCostReferenceInvalid);
        }
    }
}
