using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Application.Dto.Catalog.Cost;

namespace Application.Dto.Catalog.Equipment
{
    public class EquipmentProcessGroupDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class EquipmentDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public double CurrentPrice { get; set; }
        public IList<EquipmentProcessGroupDto> ProcessGroups { get; set; } = new List<EquipmentProcessGroupDto>();
    }

    public class EquipmentExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã thiết bị")]
        public string Code { get; set; }
        [Display(Name = "Tên thiết bị")]
        public string Name { get; set; }
        [Display(Name = "Mã nhóm công đoạn sản xuất")]
        public string ProcessGroupCodes { get; set; }
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; }
        [Display(Name = "Đơn giá")]
        public string Cost { get; set; }
    }

    public class EquipmentDetailDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public IList<ElectricityCostDto> Costs { get; set; } = new List<ElectricityCostDto>();
        public IList<EquipmentProcessGroupDto> ProcessGroups { get; set; } = new List<EquipmentProcessGroupDto>();
    }

    public class CreateEquipmentDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public IList<ElectricityCostDto> Costs { get; set; } = new List<ElectricityCostDto>();
        public IList<Guid> ProcessGroupIds { get; set; } = new List<Guid>();
    }

    public class UpdateEquipmentDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public IList<ElectricityCostDto> Costs { get; set; } = new List<ElectricityCostDto>();
        public IList<Guid> ProcessGroupIds { get; set; } = new List<Guid>();
    }
}
