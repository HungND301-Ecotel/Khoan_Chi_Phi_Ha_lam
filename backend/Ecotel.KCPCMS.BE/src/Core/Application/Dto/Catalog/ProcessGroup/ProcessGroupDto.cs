using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.ProcessGroup
{
    public class ProcessGroupDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public ProcessGroupType Type { get; set; }
        public string Name { get; set; }
    }

    public class ProcessGroupExcelDto
    {
        public Guid Id { get; set; }
        [Display(Name = "Mã nhóm công đoạn sản xuất")]
        public string Code { get; set; }
        [Display(Name = "Tên nhóm công đoạn sản xuất")]
        public string Name { get; set; }
    }

    public class CreateProcessGroupDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
