using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Microsoft.EntityFrameworkCore;
using Ardalis.Specification;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Specifications;

public class WasteSuctionTruckUnitPricesByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice, WasteSuctionTruckUnitPriceDto>
{
    public WasteSuctionTruckUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(w => w.AssignmentCode)
            .Include(w => w.ProductionProcess)
            .Include(w => w.Details).ThenInclude(d => d.HaulDistance)
            .Where(w =>
                string.IsNullOrWhiteSpace(searchTerm) ||
                (w.AssignmentCode != null && w.AssignmentCode.Name.ToLower().Contains(searchTerm)));

        Query.Select(w => new WasteSuctionTruckUnitPriceDto
        {
            Id = w.Id,
            AssignmentCodeId = w.AssignmentCodeId,
            AssignmentCodeName = w.AssignmentCode != null ? w.AssignmentCode.Name : string.Empty,
            EquipmentQuality = w.EquipmentQuality,
            ProductionProcessId = w.ProductionProcessId,
            ProductionProcessName = w.ProductionProcess != null ? w.ProductionProcess.Name : string.Empty,
            StartMonth = w.StartMonth,
            EndMonth = w.EndMonth,
            Details = w.Details.Select(d => new WasteSuctionTruckUnitPriceDetailDto
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