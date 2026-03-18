namespace Application.Dto.Catalog.SlideUnitPriceAssignmentCode
{
    public class MaterialCostDto
    {
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string UnitOfMeasureName { get; set; } = string.Empty;
        public double Cost { get; set; }
        public double Amount { get; set; }
    }
}
