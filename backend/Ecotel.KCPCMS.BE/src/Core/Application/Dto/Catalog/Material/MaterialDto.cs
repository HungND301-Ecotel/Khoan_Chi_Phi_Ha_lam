using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Application.Dto.Catalog.Cost;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.Material
{
    public class MaterialDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid AssignmentCodeId { get; set; } = Guid.Empty;
        public string AssignmentCode { get; set; } = "";
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public decimal UsageTime { get; set; }
        public MaterialType MaterialType { get; set; }
        public double CostAmount { get; set; }
    }

    public class MaterialExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã giao khoán")]
        public string AssignmentCode { get; set; }
        [Display(Name = "Mã vật tư, tài sản")]
        public string Code { get; set; }
        [Display(Name = "Tên vật tư, tài sản")]
        public string Name { get; set; }
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; }
        [Display(Name = "Loại vật tư")]
        public string MaterialType { get; set; }
        [Display(Name = "Thời gian sử dụng")]
        public decimal UsageTime { get; set; }
        [Display(Name = "Đơn giá")]
        public string Cost { get; set; }
    }

    public class MaterialDetailDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal UsageTime { get; set; }
        public Guid? AssigmentCodeId { get; set; } = Guid.Empty;
        public string AssigmentCode { get; set; } = "";
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; } = "";
        public MaterialType MaterialType { get; set; }
        public IList<MaterialCostDto> Costs { get; set; } = new List<MaterialCostDto>();
    }

    public class CreateMaterialDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? AssigmentCodeId { get; set; }
        public decimal UsageTime { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public MaterialType MaterialType { get; set; } = MaterialType.MaterialInContract;
        public IList<MaterialCostDto> Costs { get; set; } = new List<MaterialCostDto>();
    }

    public class UpdateMaterialDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? AssigmentCodeId { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public decimal UsageTime { get; set; }
        public MaterialType MaterialType { get; set; } = MaterialType.MaterialInContract;
        public IList<MaterialCostDto> Costs { get; set; } = new List<MaterialCostDto>();
    }
}
