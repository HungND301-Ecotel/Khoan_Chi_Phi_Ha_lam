using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.AssignmentCode
{
    public class AssignmentCodeDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public string UnitOfMeasureName { get; set; }
        public IList<AssignmentCodeMaterialDto> Materials { get; set; } = new List<AssignmentCodeMaterialDto>();
    }

    public class AssignmentCodeMaterialDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UnitOfMeasureName { get; set; } = string.Empty;
        public double CostAmount { get; set; }
        public double ActualAmount { get; set; }
    }

    public class ShortAssignmentCodeDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class AssignmentCodeExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã giao khoán")]
        public string Code { get; set; }
        [Display(Name = "Tên giao khoán")]
        public string Name { get; set; }
        [Display(Name = "Mã vật tư, tài sản")]
        public string MaterialCode { get; set; } = string.Empty;
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; } = string.Empty;
    }

    public class CreateAssignmentCodeDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public IList<Guid> MaterialIds { get; set; } = new List<Guid>();
    }

    public class UpdateAssignmentCodeDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
        public IList<Guid> MaterialIds { get; set; } = new List<Guid>();
    }
}
