using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.MaterialUnitPrice
{
    /// <summary>
    /// DTO chung cho tất cả loại MaterialUnitPrice (Tunnel Excavation, Longwall, v.v.)
    /// </summary>
    public class AllMaterialUnitPricesDto : IDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DefaultIdType ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double TotalPrice { get; set; }
        public DefaultIdType? TechnologyId { get; set; }
        public string? TechnologyName { get; set; }
        public MaterialUnitPriceType Type { get; set; }

        // Longwall Properties
        public DefaultIdType? LongwallParametersId { get; set; }
        public string? LongwallParametersName { get; set; }
        public DefaultIdType? CuttingThicknessId { get; set; }
        public string? CuttingThicknessName { get; set; }
        public DefaultIdType? SeamFaceId { get; set; }
        public string? SeamFaceName { get; set; }
        public DefaultIdType? PowerId { get; set; }
        public string? PowerName { get; set; }
        public bool? IsLongwallMaterialUnitPriceCGH { get; set; }

        // TunnelExcavation Properties
        public DefaultIdType? PassportId { get; set; }
        public string? PassportName { get; set; }
        public DefaultIdType? HardnessId { get; set; }
        public string? HardnessName { get; set; }
        public DefaultIdType? InsertItemId { get; set; }
        public string? InsertItemName { get; set; }
        public DefaultIdType? SupportStepId { get; set; }
        public string? SupportStepName { get; set; }
    }
}
