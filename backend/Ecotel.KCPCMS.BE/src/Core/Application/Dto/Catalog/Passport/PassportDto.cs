using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.Passport
{
    public class PassportDto : IDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Sd { get; set; }
        public string Sc { get; set; }
    }

    public class PassportExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "H/c")]
        public string Name { get; set; }
        [Display(Name = "Sđ")]
        public string Sd { get; set; }
        public string Sc { get; set; }
    }

    public class CreatePassportDto
    {
        public string Name { get; set; }
        public string Sd { get; set; }
        public string Sc { get; set; }
    }
}
