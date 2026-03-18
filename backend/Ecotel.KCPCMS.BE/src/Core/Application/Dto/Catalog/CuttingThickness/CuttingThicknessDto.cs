using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.CuttingThickness
{
    public class CuttingThicknessDto : IDto
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
    }

    public class CuttingThicknessExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Value")]
        public string Value { get; set; }
    }

    public class CreateCuttingThicknessDto
    {
        public string Value { get; set; }
    }
}
