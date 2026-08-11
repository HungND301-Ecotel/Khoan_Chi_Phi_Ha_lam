using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Microsoft.EntityFrameworkCore;
using Ardalis.Specification;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Specifications;

public class ScaniaTruckUnitPricesByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice, ScaniaTruckUnitPriceDto>
{
    public ScaniaTruckUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(s => s.AssignmentCode)
            .Include(s => s.ProductionProcess)
            .Include(s => s.CargoType)
            .Include(s => s.ReceivingLocation)
            .Include(s => s.DumpingLocation)
            .Include(s => s.Details).ThenInclude(d => d.HaulDistance)
            .Where(s =>
                string.IsNullOrWhiteSpace(searchTerm) ||
                (s.AssignmentCode != null && s.AssignmentCode.Name.ToLower().Contains(searchTerm)));

        Query.Select(s => new ScaniaTruckUnitPriceDto
        {
            Id = s.Id,
            AssignmentCodeId = s.AssignmentCodeId,
            AssignmentCodeName = s.AssignmentCode != null ? s.AssignmentCode.Name : string.Empty,
            EquipmentQuality = s.EquipmentQuality,
            ProductionProcessId = s.ProductionProcessId,
            ProductionProcessName = s.ProductionProcess != null ? s.ProductionProcess.Name : string.Empty,
            CargoTypeId = s.CargoTypeId,
            CargoTypeName = s.CargoType != null ? s.CargoType.Name : string.Empty,
            ReceivingLocationId = s.ReceivingLocationId,
            ReceivingLocationName = s.ReceivingLocation != null ? s.ReceivingLocation.Name : null,
            DumpingLocationId = s.DumpingLocationId,
            DumpingLocationName = s.DumpingLocation != null ? s.DumpingLocation.Name : null,
            StartMonth = s.StartMonth,
            EndMonth = s.EndMonth,
            Details = s.Details.Select(d => new ScaniaTruckUnitPriceDetailDto
            {
                Id = d.Id,
                HaulDistanceId = d.HaulDistanceId,
                HaulDistanceValue = d.HaulDistance != null ? d.HaulDistance.Value : null,
                FuelUnitPrice = d.FuelUnitPrice,
                PowerUnitPrice = d.PowerUnitPrice,
                MaintenanceUnitPrice = d.MaintenanceUnitPrice
            }).ToList()
        });
    }
}