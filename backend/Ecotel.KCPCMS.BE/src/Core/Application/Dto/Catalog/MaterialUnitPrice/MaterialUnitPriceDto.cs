using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.MaterialUnitPrice
{
    public class MaterialUnitPriceDto : IDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public DefaultIdType PassportId { get; set; }
        public string PassportName { get; set; } = string.Empty;
        public DefaultIdType HardnessId { get; set; }
        public string HardnessName { get; set; } = string.Empty;
        public DefaultIdType InsertItemId { get; set; }
        public string InsertItemName { get; set; } = string.Empty;
        public DefaultIdType SupportStepId { get; set; }
        public string SupportStepName { get; set; } = string.Empty;
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double TotalPrice { get; set; }
        public TunnelExcavationTrimingUnitPriceType Type { get; set; }
    }

    public class TunnelSupportAndDrillingMaterialUnitPriceDto : IDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public DefaultIdType PassportId { get; set; }
        public string PassportName { get; set; } = string.Empty;
        public DefaultIdType HardnessId { get; set; }
        public string HardnessName { get; set; } = string.Empty;
        public DefaultIdType? TechnologyId { get; set; }
        public string TechnologyName { get; set; } = string.Empty;
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double TotalPrice { get; set; }
    }

    public class CreateMaterialUnitPriceDto
    {
        public string Code { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public DefaultIdType PassportId { get; set; }
        public DefaultIdType HardnessId { get; set; }
        public DefaultIdType InsertItemId { get; set; }
        public DefaultIdType SupportStepId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double OtherMaterialValue { get; set; }
        public TunnelExcavationTrimingUnitPriceType Type { get; set; } = TunnelExcavationTrimingUnitPriceType.TunnelExcavation;
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }

    public class CreateTunnelSupportAndDrillingMaterialUnitPriceDto
    {
        public string Code { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public DefaultIdType PassportId { get; set; }
        public DefaultIdType HardnessId { get; set; }
        public DefaultIdType TechnologyId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double OtherMaterialValue { get; set; }
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }

    public class MaterialUnitPriceAssignmentCodeDto
    {
        public DefaultIdType AssignmentCodeId { get; set; }
        public string AssignmentCode { get; set; } = string.Empty;
        public string AssignmentCodeName { get; set; } = string.Empty;
        public DefaultIdType? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string UnitOfMeasureName { get; set; } = string.Empty;
        public double UnitPrice { get; set; }
        public double Norm { get; set; }
        public double TotalPrice { get; set; }
    }

    public class UpdateMaterialUnitPriceDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public DefaultIdType PassportId { get; set; }
        public DefaultIdType HardnessId { get; set; }
        public DefaultIdType InsertItemId { get; set; }
        public DefaultIdType SupportStepId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double OtherMaterialValue { get; set; }
        public TunnelExcavationTrimingUnitPriceType Type { get; set; } = TunnelExcavationTrimingUnitPriceType.TunnelExcavation;
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }

    public class UpdateTunnelSupportAndDrillingMaterialUnitPrice
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public DefaultIdType PassportId { get; set; }
        public DefaultIdType HardnessId { get; set; }
        public DefaultIdType TechnologyId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double OtherMaterialValue { get; set; }
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }

    public class MaterialUnitPriceDetailDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public DefaultIdType PassportId { get; set; }
        public DefaultIdType HardnessId { get; set; }
        public DefaultIdType InsertItemId { get; set; }
        public DefaultIdType SupportStepId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double TotalPrice { get; set; }
        public double OtherMaterialValue { get; set; }
        public TunnelExcavationTrimingUnitPriceType Type { get; set; }
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }

    public class TunnelSupportAndDrillingMaterialUnitPriceDetailDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public DefaultIdType PassportId { get; set; }
        public DefaultIdType HardnessId { get; set; }
        public DefaultIdType? TechnologyId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double TotalPrice { get; set; }
        public double OtherMaterialValue { get; set; }
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }
}
