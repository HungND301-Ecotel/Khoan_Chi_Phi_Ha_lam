using Domain.Common.Contracts;
using Domain.Entities.Index;
using MaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.MaterialUnitPrice;

namespace Domain.Entities.Pricing;

public class PlannedMaterialCost : AuditableEntity<Guid>
{
    public Guid ProductUnitPriceId { get; protected set; }
    public Guid MaterialUnitPriceId { get; protected set; }
    public Guid? SlideUnitPriceAssignmentCodeId { get; protected set; }
    public Guid? MaterialReferenceId { get; protected set; }
    public Guid? NormFactorId { get; protected set; }
    public Guid? StoneClampRatioReferenceId { get; protected set; }
    public Guid OutputId { get; protected set; }

    private double? CachedPlannedMaterialTotal { get; set; }

    //Navigation Properties
    public virtual ProductUnitPrice? ProductUnitPrice { get; protected set; }
    public virtual Output Output { get; protected set; }
    public virtual MaterialUnitPriceEntity? MaterialUnitPrice { get; protected set; }
    public virtual SlideUnitPriceAssignmentCode? SlideUnitPriceAssignmentCode { get; protected set; }
    public virtual NormFactor? NormFactor { get; protected set; }
    public virtual StoneClampRatio? StoneClampRatio { get; protected set; }
    public virtual Material? Material { get; protected set; }

    //Constructor
    public double GetTotalPrice()
    {
        if (CachedPlannedMaterialTotal.HasValue)
        {
            return CachedPlannedMaterialTotal.Value;
        }

        double slideCost = 0;
        double coefficientValue = 1;

        if (SlideUnitPriceAssignmentCodeId != null)
        {
            slideCost = SlideUnitPriceAssignmentCode?.Amount ?? 0;

            var assignmentCodeId = SlideUnitPriceAssignmentCode?.Material?.AssigmentCodeId;
            if (assignmentCodeId.HasValue && NormFactor != null)
            {
                var matchedNormFactor = NormFactor.NormFactorAssignmentCodes
                    .FirstOrDefault(x => x.AssignmentCodeId == assignmentCodeId.Value);

                if (matchedNormFactor != null)
                {
                    coefficientValue = matchedNormFactor.Value;
                }
            }
        }

        var materialCost = MaterialUnitPrice?.GetCurrentTotalPrice(Output.StartMonth) ?? 0;
        CachedPlannedMaterialTotal = (slideCost + materialCost) * coefficientValue;

        return CachedPlannedMaterialTotal.Value;
    }

    public static PlannedMaterialCost Create(Guid productUnitPriceId, Guid materialUnitPriceId, Guid? slideUnitPriceAssignmentCodeId, Guid? normFactorId, Guid? stoneClampRatioReferenceId, Guid? materialReferenceId, Guid outputId)
    {
        return new PlannedMaterialCost
        {
            ProductUnitPriceId = productUnitPriceId,
            MaterialUnitPriceId = materialUnitPriceId,
            SlideUnitPriceAssignmentCodeId = slideUnitPriceAssignmentCodeId,
            NormFactorId = normFactorId,
            StoneClampRatioReferenceId = stoneClampRatioReferenceId,
            MaterialReferenceId = materialReferenceId,
            OutputId = outputId
        };
    }

    public void Update(Guid materialUnitPriceId, Guid? slideUnitPriceAssignmentCodeId,
        Guid? normFactorId, Guid? stoneClampRatioReferenceId, Guid? materialReferenceId, Guid outputId)
    {
        MaterialUnitPriceId = materialUnitPriceId;
        SlideUnitPriceAssignmentCodeId = slideUnitPriceAssignmentCodeId;
        NormFactorId = normFactorId;
        StoneClampRatioReferenceId = stoneClampRatioReferenceId;
        MaterialReferenceId = materialReferenceId;
        OutputId = outputId;
    }
}
