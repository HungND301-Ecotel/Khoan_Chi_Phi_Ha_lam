using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Application.Dto.Catalog.UnitOfMeasure
{
    public class UnitOfMeasureDto : IDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
    public class UnitOfMeasureExcelDto
    {
        public Guid Id { get; set; }

        [Display(Name = "Đơn vị tính")]
        public string Name { get; set; }
    }

    public class ImportDto
    {
        public IFormFile FormFile { get; set; }
    }

    public class CreateUnitOfMeasureDto
    {
        public string Name { get; set; }
    }
}
