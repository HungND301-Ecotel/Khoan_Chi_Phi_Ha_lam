using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MaterialUnitPrice;

public class LongwallMaterialUnitPrice : MaterialUnitPrice
{
    public Guid LongwallParametersId { get; protected set; }
    public Guid CuttingThicknessId { get; protected set; }
    public Guid SeamFaceId { get; protected set; }

    // Navigation properties
    public virtual LongwallParameters? LongwallParameters { get; protected set; }
    public virtual CuttingThickness? CuttingThickness { get; protected set; }
    public virtual SeamFace? SeamFace { get; protected set; }

    public static LongwallMaterialUnitPrice Create(
        string code,
        Guid processId,
        Guid longwallParametersId,
        Guid cuttingThicknessId,
        Guid seamFaceId,
        Guid? technologyId,
        DateOnly startMonth,
        DateOnly endMonth,
        double otherMaterialvalue,
        IList<MaterialUnitPriceAssignmentCode> costs)
    {
        ValidateCommonFields(code, startMonth, endMonth);

        var materialUnitPrice = new LongwallMaterialUnitPrice
        {
            Code = new Code(code.ToUpper()),
            ProcessId = processId,
            LongwallParametersId = longwallParametersId,
            CuttingThicknessId = cuttingThicknessId,
            SeamFaceId = seamFaceId,
            TechnologyId = technologyId,
            OtherMaterialvalue = otherMaterialvalue,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
        };

        materialUnitPrice.AddCosts(costs);
        return materialUnitPrice;
    }

    public void Update(
        string code,
        Guid processId,
        Guid longwallParametersId,
        Guid cuttingThicknessId,
        Guid seamFaceId,
        Guid? technologyId,
        DateOnly startMonth,
        DateOnly endMonth,
        double otherMaterialvalue,
        IList<MaterialUnitPriceAssignmentCode> costs)
    {
        ValidateCommonFields(code, startMonth, endMonth);

        if (Code != null)
        {
            Code.Value = code.ToUpper();
        }

        ProcessId = processId;
        LongwallParametersId = longwallParametersId;
        CuttingThicknessId = cuttingThicknessId;
        SeamFaceId = seamFaceId;
        TechnologyId = technologyId;
        StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
        EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
        OtherMaterialvalue = otherMaterialvalue;
        this.AddCosts(costs);
    }
}
