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
    }

    public class AssignmentCodeExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã giao khoán")]
        public string Code { get; set; }
        [Display(Name = "Tên giao khoán")]
        public string Name { get; set; }
        [Display(Name = "Đơn vị tính")]
        public string UnitOfMeasureName { get; set; } = string.Empty;
    }

    public class CreateAssignmentCodeDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
    }

    public class UpdateAssignmentCodeDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
    }
}
