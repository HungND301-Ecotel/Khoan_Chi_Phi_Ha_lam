using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.LongwallParameters
{
    public class LongwallParametersDto : IDto
    {
        public Guid Id { get; set; }
        public string Llc { get; set; }
        public string Lkc { get; set; }
        public string Mk { get; set; }
    }

    public class LongwallParametersExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "LLC")]
        public string Llc { get; set; }
        [Display(Name = "LKC")]
        public string Lkc { get; set; }
        [Display(Name = "MK")]
        public string Mk { get; set; }
    }

    public class CreateLongwallParametersDto
    {
        public string Llc { get; set; }
        public string Lkc { get; set; }
        public string Mk { get; set; }
    }
}
