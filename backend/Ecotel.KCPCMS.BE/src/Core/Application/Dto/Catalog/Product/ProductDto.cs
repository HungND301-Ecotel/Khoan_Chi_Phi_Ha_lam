using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.Product
{
    public class ProductDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public Guid ProcessGroupId { get; set; }
        public string ProcessGroupCode { get; set; } = "";
        public string ProcessGroupName { get; set; } = "";
    }

    public class ProductExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã nhóm CĐSX")]
        public string ProcessGroupCode { get; set; } = "";
        [Display(Name = "Mã sản phẩm")]
        public string Code { get; set; } = "";
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = "";
    }

    public class UpdateProductDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public Guid ProcessGroupId { get; set; }
    }

    public class CreateProductDto
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public Guid ProcessGroupId { get; set; }
    }
}
