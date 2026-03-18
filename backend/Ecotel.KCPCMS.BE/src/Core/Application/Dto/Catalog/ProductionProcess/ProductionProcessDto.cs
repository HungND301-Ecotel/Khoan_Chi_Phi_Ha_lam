using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.ProductionProcess
{
    public class ProductionProcessDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid ProcessGroupId { get; set; }
        public string ProcessGroupName { get; set; }
    }

    public class ProductionProcessExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã công đoạn sản xuất")]
        public string Code { get; set; }
        [Display(Name = "Tên công đoạn sản xuất")]
        public string Name { get; set; }
        [Display(Name = "Mã nhóm công đoạn sản xuất")]
        public string ProcessGroupCode { get; set; }
    }

    public class CreateProductionProcessDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid ProcessGroupId { get; set; }
    }

    public class UpdateProductionProcessDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid ProcessGroupId { get; set; }
    }
}
