using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MaterialUnitPrice;

public class TunnelExcavationMaterialUnitPrice : MaterialUnitPrice
{
    public Guid PassportId { get; protected set; }
    public Guid HardnessId { get; protected set; }
    public Guid InsertItemId { get; protected set; }
    public Guid SupportStepId { get; protected set; }

    // Navigation properties
    public virtual Passport? Passport { get; protected set; }
    public virtual Hardness? Hardness { get; protected set; }
    public virtual InsertItem? InsertItem { get; protected set; }
    public virtual SupportStep? SupportStep { get; protected set; }

    public static TunnelExcavationMaterialUnitPrice Create(
        string code,
        Guid processId,
        Guid passportId,
        Guid hardnessId,
        Guid insertItemId,
        Guid supportStepId,
        Guid? technologyId,
        DateOnly startMonth,
        DateOnly endMonth,
        double OtherMaterialvalue,
        IList<MaterialUnitPriceAssignmentCode> costs)
    {
        ValidateCommonFields(code, startMonth, endMonth);


        var materialUnitPrice = new TunnelExcavationMaterialUnitPrice
        {
            Code = new Code(code.ToUpper()),
            ProcessId = processId,
            PassportId = passportId,
            HardnessId = hardnessId,
            InsertItemId = insertItemId,
            SupportStepId = supportStepId,
            TechnologyId = technologyId,
            OtherMaterialvalue = OtherMaterialvalue,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
        };

        materialUnitPrice.AddCosts(costs);
        return materialUnitPrice;
    }

    public void Update(
        string code,
        Guid processId,
        Guid passportId,
        Guid hardnessId,
        Guid insertItemId,
        Guid supportStepId,
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
        PassportId = passportId;
        HardnessId = hardnessId;
        InsertItemId = insertItemId;
        SupportStepId = supportStepId;
        TechnologyId = technologyId;
        StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
        EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
        OtherMaterialvalue = otherMaterialvalue;
        this.AddCosts(costs);
    }
}
