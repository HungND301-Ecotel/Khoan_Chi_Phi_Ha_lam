using Domain.Common.Contracts;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing
{
    public class TransportUnitPrice : AuditableEntity<Guid>, IAggregateRoot
    {
        public Guid ProductionProcessId { get; protected set; }
        public Guid? TransportRouteId { get; protected set; }
        public Guid? DepartmentId { get; protected set; }
        public Guid? EquipmentId { get; protected set; }
        public string? EquipmentQuality { get; protected set; }
        public decimal? MaterialFuelUnitPrice { get; protected set; }
        public decimal? PowerUnitPrice { get; protected set; }
        public decimal? MaintenanceUnitPrice { get; protected set; }
        public decimal? Quantity { get; protected set; }
        public bool IsLowVolumeCase { get; protected set; }
        public DateOnly StartMonth { get; protected set; }
        public DateOnly EndMonth { get; protected set; }

        public virtual ProductionProcess? ProductionProcess { get; protected set; }
        public virtual TransportRoute? TransportRoute { get; protected set; }
        public virtual Department? Department { get; protected set; }
        public virtual AssignmentCode? Equipment { get; protected set; }

        public static TransportUnitPrice Create(
            Guid productionProcessId,
            Guid? transportRouteId,
            Guid? departmentId,
            Guid? equipmentId,
            string? equipmentQuality,
            decimal? materialFuelUnitPrice,
            decimal? powerUnitPrice,
            decimal? maintenanceUnitPrice,
            decimal? quantity,
            bool isLowVolumeCase,
            DateOnly startMonth,
            DateOnly endMonth)
        {
            Validate(productionProcessId, materialFuelUnitPrice, powerUnitPrice, maintenanceUnitPrice, startMonth, endMonth);

            return new TransportUnitPrice
            {
                ProductionProcessId = productionProcessId,
                TransportRouteId = transportRouteId,
                DepartmentId = departmentId,
                EquipmentId = equipmentId,
                EquipmentQuality = equipmentQuality,
                MaterialFuelUnitPrice = materialFuelUnitPrice,
                PowerUnitPrice = powerUnitPrice,
                MaintenanceUnitPrice = maintenanceUnitPrice,
                Quantity = quantity,
                IsLowVolumeCase = isLowVolumeCase,
                StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
                EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            };
        }

        public static TransportUnitPrice Create(
            Guid id,
            Guid productionProcessId,
            Guid? transportRouteId,
            Guid? departmentId,
            Guid? equipmentId,
            string? equipmentQuality,
            decimal? materialFuelUnitPrice,
            decimal? powerUnitPrice,
            decimal? maintenanceUnitPrice,
            decimal? quantity,
            bool isLowVolumeCase,
            DateOnly startMonth,
            DateOnly endMonth)
        {
            Validate(productionProcessId, materialFuelUnitPrice, powerUnitPrice, maintenanceUnitPrice, startMonth, endMonth);

            return new TransportUnitPrice
            {
                Id = id,
                ProductionProcessId = productionProcessId,
                TransportRouteId = transportRouteId,
                DepartmentId = departmentId,
                EquipmentId = equipmentId,
                EquipmentQuality = equipmentQuality,
                MaterialFuelUnitPrice = materialFuelUnitPrice,
                PowerUnitPrice = powerUnitPrice,
                MaintenanceUnitPrice = maintenanceUnitPrice,
                Quantity = quantity,
                IsLowVolumeCase = isLowVolumeCase,
                StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
                EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            };
        }

        public void Update(
            Guid productionProcessId,
            Guid? transportRouteId,
            Guid? departmentId,
            Guid? equipmentId,
            string? equipmentQuality,
            decimal? materialFuelUnitPrice,
            decimal? powerUnitPrice,
            decimal? maintenanceUnitPrice,
            decimal? quantity,
            bool isLowVolumeCase,
            DateOnly startMonth,
            DateOnly endMonth)
        {
            Validate(productionProcessId, materialFuelUnitPrice, powerUnitPrice, maintenanceUnitPrice, startMonth, endMonth);

            ProductionProcessId = productionProcessId;
            TransportRouteId = transportRouteId;
            DepartmentId = departmentId;
            EquipmentId = equipmentId;
            EquipmentQuality = equipmentQuality;
            MaterialFuelUnitPrice = materialFuelUnitPrice;
            PowerUnitPrice = powerUnitPrice;
            MaintenanceUnitPrice = maintenanceUnitPrice;
            Quantity = quantity;
            IsLowVolumeCase = isLowVolumeCase;
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
        }

        public bool CheckChange(TransportUnitPrice dto)
        {
            return !(ProductionProcessId == dto.ProductionProcessId
                && TransportRouteId == dto.TransportRouteId
                && DepartmentId == dto.DepartmentId
                && EquipmentId == dto.EquipmentId
                && EquipmentQuality == dto.EquipmentQuality
                && MaterialFuelUnitPrice == dto.MaterialFuelUnitPrice
                && PowerUnitPrice == dto.PowerUnitPrice
                && MaintenanceUnitPrice == dto.MaintenanceUnitPrice
                && Quantity == dto.Quantity
                && IsLowVolumeCase == dto.IsLowVolumeCase
                && StartMonth == dto.StartMonth
                && EndMonth == dto.EndMonth);
        }

        private static void Validate(
            Guid productionProcessId,
            decimal? materialFuelUnitPrice,
            decimal? powerUnitPrice,
            decimal? maintenanceUnitPrice,
            DateOnly startMonth,
            DateOnly endMonth)
        {
            if (productionProcessId == Guid.Empty)
            {
                throw new ArgumentException(CustomResponseMessage.ProductionProcessIdCannotBeEmpty);
            }

            if (startMonth > endMonth)
            {
                throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
            }

            if (!materialFuelUnitPrice.HasValue && !powerUnitPrice.HasValue && !maintenanceUnitPrice.HasValue)
            {
                throw new ArgumentException(CustomResponseMessage.TransportUnitPriceMustHaveAtLeastOneValue);
            }
        }
    }
}