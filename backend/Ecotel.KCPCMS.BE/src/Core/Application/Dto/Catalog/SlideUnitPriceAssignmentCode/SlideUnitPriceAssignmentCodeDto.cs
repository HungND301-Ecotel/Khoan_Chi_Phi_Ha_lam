namespace Application.Dto.Catalog.SlideUnitPriceAssignmentCode
{

    public class CreateSlideUnitPriceAssignmentCodeDto
    {
        public Guid AssignmentCodeId { get; set; }
        public Guid MaterialId { get; set; }
        public double? Amount { get; set; }
    }

    public class SlideUnitPriceAssignmentCodeDetailDto
    {
        public Guid AssignmentCodeId { get; set; }
        public string AssignmentCode { get; set; } = string.Empty;
        public string AssignmentCodeName { get; set; } = string.Empty;
        public IList<MaterialCostDto> Costs { get; set; } = new List<MaterialCostDto>();
    }
}
