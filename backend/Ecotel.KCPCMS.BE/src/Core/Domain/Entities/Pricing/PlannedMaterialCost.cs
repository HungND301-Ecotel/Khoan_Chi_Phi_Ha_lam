using Domain.Common.Contracts;
using Domain.Entities.Index;
using MaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.MaterialUnitPrice;

namespace Domain.Entities.Pricing;

public class PlannedMaterialCost : AuditableEntity<Guid>
{
    public Guid ProductUnitPriceId { get; protected set; }
    public Guid MaterialUnitPriceId { get; protected set; }
    public Guid? SlideUnitPriceAssignmentCodeId { get; protected set; }
    public Guid? StoneClampRatioId { get; protected set; }
    public Guid OutputId { get; protected set; }

    private double? CachedPlannedMaterialTotal { get; set; }

    //Navigation Properties
    public virtual ProductUnitPrice? ProductUnitPrice { get; protected set; }
    public virtual Output Output { get; protected set; }
    public virtual MaterialUnitPriceEntity? MaterialUnitPrice { get; protected set; }
    public virtual SlideUnitPriceAssignmentCode? SlideUnitPriceAssignmentCode { get; protected set; }
    public virtual StoneClampRatio? StoneClampRatio { get; protected set; }

    //Constructor
    public double GetTotalPrice()
    {
        if (CachedPlannedMaterialTotal.HasValue)
        {
            return CachedPlannedMaterialTotal.Value;
        }
        double SlideCost = 0;
        if (SlideUnitPriceAssignmentCodeId != null)
        {
            SlideCost = SlideUnitPriceAssignmentCode?.Amount ?? 0;
        }
        CachedPlannedMaterialTotal = (SlideCost + MaterialUnitPrice?.GetCurrentTotalPrice(Output.StartMonth) ?? 0) * (StoneClampRatio?.CoefficientValue ?? 1);

        return CachedPlannedMaterialTotal.Value;
    }

    public static PlannedMaterialCost Create(Guid productUnitPriceId, Guid materialUnitPriceId, Guid? slideUnitPriceAssignmentCodeId, Guid? stoneClampRatioId, Guid outputId)
    {
        return new PlannedMaterialCost
        {
            ProductUnitPriceId = productUnitPriceId,
            MaterialUnitPriceId = materialUnitPriceId,
            SlideUnitPriceAssignmentCodeId = slideUnitPriceAssignmentCodeId,
            StoneClampRatioId = stoneClampRatioId,
            OutputId = outputId
        };
    }

    public void Update(Guid materialUnitPriceId, Guid? slideUnitPriceAssignmentCodeId,
        Guid? stoneClampRatioId, Guid outputId)
    {
        MaterialUnitPriceId = materialUnitPriceId;
        SlideUnitPriceAssignmentCodeId = slideUnitPriceAssignmentCodeId;
        StoneClampRatioId = stoneClampRatioId;
        OutputId = outputId;
    }
}
