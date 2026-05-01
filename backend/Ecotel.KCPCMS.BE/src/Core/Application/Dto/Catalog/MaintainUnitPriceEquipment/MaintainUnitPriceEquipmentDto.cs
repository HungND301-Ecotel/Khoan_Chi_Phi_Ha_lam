using Application.Common.Interfaces;
using Application.Dto.Catalog.Equipment;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.MaintainUnitPriceEquipment
{
    public class ShortMaintainUnitPriceDto : IDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public string EquipmentCode { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double? OtherMaterialValue { get; set; }
        public double TotalPrice { get; set; }
        public MaintainUnitPriceType Type { get; set; }
    }

    public class MaintainUnitPriceDto : IDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public string EquipmentCode { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double? OtherMaterialValue { get; set; }
        public double TotalPrice { get; set; }
        public MaintainUnitPriceType Type { get; set; }

        public IList<MaintainUnitPriceEquipmentDto> MaintainUnitPriceEquipment { get; set; } =
            new List<MaintainUnitPriceEquipmentDto>();
    }

    public class MaintainUnitPriceEquipmentDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public string EquipmentCode { get; set; }
        public Guid PartId { get; set; }
        public PartType PartType { get; set; }
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public double PartCost { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public decimal AverageMonthlyTunnelProduction { get; set; }
        public double Quantity { get; set; }
        public double MaterialRatePerMetres { get; set; }
        public double MaterialCostPerMetres { get; set; }
    }

    public class CreateMaintainUnitPriceEquipmentDto
    {
        public Guid PartId { get; set; }
        public double? Quantity { get; set; }
        public decimal AverageMonthlyTunnelProduction { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
    }

    public class UpdateMaintainUnitPriceDto
    {
        public Guid EquipmentId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double? OtherMaterialValue { get; set; }
        public MaintainUnitPriceType Type { get; set; }

        public IList<UpdateMaintainUnitPriceEquipmentDto> PartUnitPrices { get; set; } =
            new List<UpdateMaintainUnitPriceEquipmentDto>();
    }

    public class UpdateMaintainUnitPriceEquipmentDto
    {
        public Guid PartId { get; set; }
        public double Quantity { get; set; }
        public decimal AverageMonthlyTunnelProduction { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
    }

    public class MaintainUnitPriceEquipmentEquipmentsDto
    {
        public Guid MaintainUnitPriceEquipmentId { get; set; }
        public IList<EquipmentDto> Equipments { get; set; } = new List<EquipmentDto>();
    }

    public class PartEquipmentsDto
    {
        public Guid PartId { get; set; }
        public IList<EquipmentDto> Equipments { get; set; } = new List<EquipmentDto>();
    }

    public class PartMaintainUnitPriceEquipmentsDto
    {
        public Guid PartId { get; set; }
        public IList<Guid> MaintainUnitPriceEquipmentIds { get; set; } =
            new List<Guid>();
    }
}


