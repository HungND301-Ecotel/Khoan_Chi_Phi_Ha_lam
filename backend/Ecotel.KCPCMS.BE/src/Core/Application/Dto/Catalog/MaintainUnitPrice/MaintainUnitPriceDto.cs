using Domain.Common.Enums;

namespace Application.Dto.Catalog.MaintainUnitPrice
{
    public class MaintainUnitPriceDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public double TotalPrice { get; set; }
    }

    public class CreateMaintainUnitPriceEquipmentDto
    {
        public Guid EquipmentId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double? OtherMaterialValue { get; set; }
        public MaintainUnitPriceType Type { get; set; }
        public IList<MaintainUnitPriceEquipment.CreateMaintainUnitPriceEquipmentDto> Costs { get; set; } = new List<MaintainUnitPriceEquipment.CreateMaintainUnitPriceEquipmentDto>();
    }
}
