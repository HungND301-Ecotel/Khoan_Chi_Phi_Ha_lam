using Application.Common.Interfaces;

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
        public double TotalPrice { get; set; }
    }

    public class MaterialUnitPriceDetailDto
    {
        public DefaultIdType Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double TotalPrice { get; set; }
    }
}
