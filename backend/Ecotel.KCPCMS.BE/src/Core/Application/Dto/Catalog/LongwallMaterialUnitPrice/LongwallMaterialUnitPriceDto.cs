using Application.Common.Interfaces;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.LongwallParameters;
using Application.Dto.Catalog.MaterialUnitPrice;

namespace Application.Dto.Catalog.LongwallMaterialUnitPrice
{
    /// <summary>
    /// DTO cho Lò chợ - Longwall Material Unit Price
    /// </summary>
    public class LongwallMaterialUnitPriceDto : IDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public string ProcessName { get; set; }
        public DefaultIdType LongwallParametersId { get; set; }
        public DefaultIdType CuttingThicknessId { get; set; }
        public DefaultIdType SeamFaceId { get; set; }
        public DefaultIdType? TechnologyId { get; set; }
        public DefaultIdType? PowerId { get; set; }
        public DefaultIdType? HardnessId { get; set; }
        public string? PowerName { get; set; }
        public string? HardnessName { get; set; }
        public bool IsLongwallMaterialUnitPriceCGH { get; set; }
        public string TechnologyName { get; set; } = "";
        public LongwallParametersDto LongwallParameters { get; set; }
        public CuttingThicknessDto CuttingThickness { get; set; }
        public string SeamFaceName { get; set; } = string.Empty;
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double TotalPrice { get; set; }
        public double OtherMaterialValue { get; set; }
        public IEnumerable<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }

    public class CreateLongwallMaterialUnitPriceDto
    {
        public string Code { get; set; } = string.Empty;
        public DefaultIdType LongwallParametersId { get; set; }
        public DefaultIdType CuttingThicknessId { get; set; }
        public DefaultIdType? SeamFaceId { get; set; }
        public DefaultIdType? PowerId { get; set; }
        public DefaultIdType? HardnessId { get; set; }
        public DefaultIdType ProcessId { get; set; }
        public DefaultIdType? TechnologyId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double OtherMaterialValue { get; set; }
        public string InterpolationSeamFaceValue { get; set; } = "";
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }

    public class UpdateLongwallMaterialUnitPriceDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DefaultIdType LongwallParametersId { get; set; }
        public DefaultIdType CuttingThicknessId { get; set; }
        public DefaultIdType? SeamFaceId { get; set; }
        public DefaultIdType? PowerId { get; set; }
        public DefaultIdType? HardnessId { get; set; }
        public DefaultIdType ProcessId { get; set; }
        public DefaultIdType? TechnologyId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double OtherMaterialValue { get; set; }
        public string InterpolationSeamFaceValue { get; set; } = "";
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }

    public class LongwallMaterialUnitPriceDetailDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public LongwallParametersDto LongwallParameters { get; set; }
        public CuttingThicknessDto CuttingThickness { get; set; }
        public DefaultIdType? SeamFaceId { get; set; }
        public DefaultIdType? TechnologyId { get; set; }
        public DefaultIdType? PowerId { get; set; }
        public DefaultIdType? HardnessId { get; set; }
        public bool IsLongwallMaterialUnitPriceCGH { get; set; }
        public DefaultIdType ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double OtherMaterialValue { get; set; }
        public IList<MaterialUnitPriceAssignmentCodeDto> Costs { get; set; } = [];
    }
}
