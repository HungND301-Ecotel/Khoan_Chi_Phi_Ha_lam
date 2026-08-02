using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.TransportUnitPrice;
using Ardalis.Specification;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Specifications;

public class TransportUnitPricesByPaginationSpec : EntitiesByPaginationFilterSpec<TransportUnitPriceEntity, TransportUnitPriceDto>
{
    public TransportUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(t => string.IsNullOrWhiteSpace(searchTerm) ||
                        (t.ProductionProcess != null && t.ProductionProcess.Name.ToLower().Contains(searchTerm)) ||
                        (t.TransportRoute != null && t.TransportRoute.Name.ToLower().Contains(searchTerm)) ||
                        (t.Equipment != null && t.Equipment.Name.ToLower().Contains(searchTerm)));
        Query
            .Select(t => new TransportUnitPriceDto
            {
                Id = t.Id,
                ProductionProcessId = t.ProductionProcessId,
                ProductionProcessName = t.ProductionProcess != null ? t.ProductionProcess.Name : string.Empty,
                TransportRouteId = t.TransportRouteId,
                TransportRouteName = t.TransportRoute != null ? t.TransportRoute.Name : null,
                DepartmentId = t.DepartmentId,
                DepartmentName = t.Department != null ? t.Department.Name : null,
                EquipmentId = t.EquipmentId,
                EquipmentName = t.Equipment != null ? t.Equipment.Name : null,
                EquipmentQuality = t.EquipmentQuality,
                MaterialFuelUnitPrice = t.MaterialFuelUnitPrice,
                PowerUnitPrice = t.PowerUnitPrice,
                MaintenanceUnitPrice = t.MaintenanceUnitPrice,
                Quantity = t.Quantity,
                IsLowVolumeCase = t.IsLowVolumeCase,
                StartMonth = t.StartMonth,
                EndMonth = t.EndMonth
            });
    }
}