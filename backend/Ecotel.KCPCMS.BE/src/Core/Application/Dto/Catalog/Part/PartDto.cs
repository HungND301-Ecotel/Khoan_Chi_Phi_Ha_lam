using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Application.Dto.Catalog.Cost;

namespace Application.Dto.Catalog.Part
{
    public class PartDto : IDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentPartId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public Guid EquipmentId { get; set; }
        public string EquipmentCode { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public double CostAmount { get; set; }
        public double ActualAmount { get; set; }
    }

    public class OtherPartDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public double CostAmount { get; set; }
        public double ActualAmount { get; set; }
    }

    public class PartExcelDto
    {
        [Display(Name = "Mã thiết bị")]
        public string EquipmentCode { get; set; }
        [Display(Name = "Tên thiết bị")]
        public string EquipmentName { get; set; }
        [Display(Name = "Mã phụ tùng")]
        public string Code { get; set; }
        [Display(Name = "Tên phụ tùng")]
        public string Name { get; set; }
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; }
        [Display(Name = "Định mức thời gian thay thế (tháng)")]
        public decimal ReplacementTimeStandard { get; set; }

        [Display(Name = "Đơn giá")]
        public string Cost { get; set; }
    }

    public class OtherPartExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã phụ tùng")]
        public string Code { get; set; }
        [Display(Name = "Tên phụ tùng")]
        public string Name { get; set; }
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; }
        [Display(Name = "Định mức thời gian thay thế (tháng)")]
        public decimal ReplacementTimeStandard { get; set; }

        [Display(Name = "Đơn giá")]
        public string Cost { get; set; }
    }

    public class PartDetailDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public IList<Guid> EquipmentIds { get; set; } = new List<Guid>();
        public IList<string> EquipmentCodes { get; set; } = new List<string>();
        public IList<MaintainCostDto> Costs { get; set; } = new List<MaintainCostDto>();
    }

    public class OtherPartDetailDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public IList<MaintainCostDto> Costs { get; set; } = new List<MaintainCostDto>();
    }

    public class PartDetailBaseDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public IList<Guid> EquipmentIds { get; set; } = new List<Guid>();
        public IList<string> EquipmentCodes { get; set; } = new List<string>();
        public double CurrentCost { get; set; }
        public double ActualAmount { get; set; }
    }

    public class CreatePartDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public IList<Guid> EquipmentIds { get; set; } = new List<Guid>();
        public IList<MaintainCostDto> Costs { get; set; } = new List<MaintainCostDto>();
    }

    public class CreateOtherPartDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public IList<MaintainCostDto> Costs { get; set; } = new List<MaintainCostDto>();
    }

    public class UpdatePartDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public IList<Guid> EquipmentIds { get; set; } = new List<Guid>();
        public IList<MaintainCostDto> Costs { get; set; } = new List<MaintainCostDto>();
    }

    public class UpdateOtherPartDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public decimal ReplacementTimeStandard { get; set; }
        public IList<MaintainCostDto> Costs { get; set; } = new List<MaintainCostDto>();
    }
}
