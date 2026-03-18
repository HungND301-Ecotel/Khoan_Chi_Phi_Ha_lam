using Application.Common.Interfaces;
using Application.Dto.Catalog.SlideUnitPriceAssignmentCode;

namespace Application.Dto.Catalog.SlideUnitPrice
{
    public class SlideUnitPriceDto : IDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public Guid ProcessGroupId { get; set; }
        public string ProcessGroupName { get; set; } = string.Empty;
        public Guid PassportId { get; set; }
        public string PassportName { get; set; } = string.Empty;
        public Guid HardnessId { get; set; }
        public string HardnessName { get; set; } = string.Empty;
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double TotalPrice { get; set; }
    }

    public class CreateSlideUnitPriceDto
    {
        public string Code { get; set; } = string.Empty;
        public Guid ProcessGroupId { get; set; }
        public Guid PassportId { get; set; }
        public Guid HardnessId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public IList<CreateSlideUnitPriceAssignmentCodeDto> Costs { get; set; } = new List<CreateSlideUnitPriceAssignmentCodeDto>();
    }

    public class UopdateSlideUnitPriceDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public Guid ProcessGroupId { get; set; }
        public Guid PassportId { get; set; }
        public Guid HardnessId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }

        public IList<CreateSlideUnitPriceAssignmentCodeDto> Costs { get; set; } = new List<CreateSlideUnitPriceAssignmentCodeDto>();
    }

    public class SlideUnitPriceDetailDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }

        public IList<SlideUnitPriceAssignmentCodeDetailDto> MaterialCost { get; set; } =
            new List<SlideUnitPriceAssignmentCodeDetailDto>();
    }
}
