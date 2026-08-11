using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Microsoft.EntityFrameworkCore;
using Ardalis.Specification;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ServiceAndCraneVehicleUnitPrice.Specifications;

public class ServiceAndCraneVehicleUnitPricesByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice, ServiceAndCraneVehicleUnitPriceDto>
{
    public ServiceAndCraneVehicleUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(s => s.AssignmentCode)
            .Include(s => s.ProductionProcess)
            .Include(s => s.Details).ThenInclude(d => d.HaulDistance)
            .Where(s =>
                string.IsNullOrWhiteSpace(searchTerm) ||
                (s.AssignmentCode != null && s.AssignmentCode.Name.ToLower().Contains(searchTerm)));

        Query.Select(s => new ServiceAndCraneVehicleUnitPriceDto
        {
            Id = s.Id,
            AssignmentCodeId = s.AssignmentCodeId,
            AssignmentCodeName = s.AssignmentCode != null ? s.AssignmentCode.Name : string.Empty,
            EquipmentQuality = s.EquipmentQuality,
            ProductionProcessId = s.ProductionProcessId,
            ProductionProcessName = s.ProductionProcess != null ? s.ProductionProcess.Name : string.Empty,
            StartMonth = s.StartMonth,
            EndMonth = s.EndMonth,
            Details = s.Details.Select(d => new ServiceAndCraneVehicleUnitPriceDetailDto
            {
                Id = d.Id,
                HaulDistanceId = d.HaulDistanceId,
                HaulDistanceValue = d.HaulDistance != null ? d.HaulDistance.Value : null,
                FuelUnitPrice = d.FuelUnitPrice,
                MaintenanceUnitPrice = d.MaintenanceUnitPrice
            }).ToList()
        });
    }
}