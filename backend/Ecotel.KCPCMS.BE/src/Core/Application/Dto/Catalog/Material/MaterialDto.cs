using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Application.Dto.Catalog.Cost;
using Domain.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.Dto.Catalog.Material
{
    public class MaterialDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid AssignmentCodeId { get; set; } = Guid.Empty;
        public bool IsSlideAssignmentCode { get; set; } = false;
        public string AssignmentCode { get; set; } = "";
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public MaterialType MaterialType { get; set; }
        public double CostAmount { get; set; }
        public double ActualAmount { get; set; }
    }

    public class MaterialExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã vật tư, tài sản")]
        public string Code { get; set; }
        [Display(Name = "Tên vật tư, tài sản")]
        public string Name { get; set; }
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; }
        [Display(Name = "Đơn giá")]
        public string Cost { get; set; }
    }
    public class ImportMaterialDto
    {
        public IFormFile FormFile { get; set; }
        public MaterialType MaterialType { get; set; } = MaterialType.MaterialInContract;
    }

    public class MaterialDetailDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
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
        public Guid? UnitOfMeasureId { get; set; }
        public MaterialType MaterialType { get; set; } = MaterialType.MaterialInContract;
        public IList<MaterialCostDto> Costs { get; set; } = new List<MaterialCostDto>();
    }

    public class UpdateMaterialDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public MaterialType MaterialType { get; set; } = MaterialType.MaterialInContract;
        public IList<MaterialCostDto> Costs { get; set; } = new List<MaterialCostDto>();
    }
}

