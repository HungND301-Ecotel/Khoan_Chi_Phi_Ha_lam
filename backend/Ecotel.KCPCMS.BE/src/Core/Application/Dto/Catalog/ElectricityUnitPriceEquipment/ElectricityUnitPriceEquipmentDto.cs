using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.ElectricityUnitPriceEquipment
{
    public class ElectricityUnitPriceEquipmentDto : IDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public string EquipmentCode { get; set; }
        public string EquipmentName { get; set; }
        public IList<ProcessGroupType> ProcessGroupTypes { get; set; } = new List<ProcessGroupType>();
        public string UnitOfMeasureName { get; set; }
        public double EquipmentElectricityCost { get; set; }
        public double ElectricityConsumePerMetres { get; set; }
        public double ElectricityCostPerMetres { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public ElectricityUnitPriceType Type { get; set; }
        
        // Tunnel Excavation (Đào lò) properties
        public double? MonthlyElectricityCost { get; set; }
        public decimal? AverageMonthlyTunnelProduction { get; set; }
        
        // Longwall (Lò chợ) properties
        public decimal? Quantity { get; set; }
        public decimal? Pdm { get; set; }
        public double? Kyc { get; set; }
        public double? Kdt { get; set; }
        public double? WorkingHour { get; set; }
        public decimal? WorkingDate { get; set; }
        public decimal? LongwallAverageMonthlyTunnelProduction { get; set; }
        
        // Longwall (Lò chợ) calculated properties
        public decimal? SPdm { get; set; }  // Quantity * Pdm - Tổng công suất định mức
        public double? Ptt { get; set; }   // SPdm * Kyc * Kdt - Công suất thực tế
    }

    public class CreateElectricityUnitPriceEquipmentDto
    {
        public Guid EquipmentId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double MonthlyElectricityCost { get; set; }
        public decimal AverageMonthlyTunnelProduction { get; set; }
        public ElectricityUnitPriceType Type { get; set; } = ElectricityUnitPriceType.TunnelExcavation;
    }

    public class UpdateElectricityUnitPriceEquipmentDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public double MonthlyElectricityCost { get; set; }
        public decimal AverageMonthlyTunnelProduction { get; set; }
        public ElectricityUnitPriceType Type { get; set; } = ElectricityUnitPriceType.TunnelExcavation;
    }

    // New DTOs for Longwall
    public class CreateLongwallElectricityUnitPriceEquipmentDto
    {
        public Guid EquipmentId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public decimal Quantity { get; set; }
        public decimal Pdm { get; set; }
        public double Kyc { get; set; }
        public double Kdt { get; set; }
        public double WorkingHour { get; set; }
        public decimal WorkingDate { get; set; }
        public decimal AverageMonthlyTunnelProduction { get; set; }
    }

    public class UpdateLongwallElectricityUnitPriceEquipmentDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentId { get; set; }
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public decimal Quantity { get; set; }
        public decimal Pdm { get; set; }
        public double Kyc { get; set; }
        public double Kdt { get; set; }
        public double WorkingHour { get; set; }
        public decimal WorkingDate { get; set; }
        public decimal AverageMonthlyTunnelProduction { get; set; }
    }
}
