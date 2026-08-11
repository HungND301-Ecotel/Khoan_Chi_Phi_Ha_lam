using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Ardalis.Specification;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Specifications;

public class ExcavatorAndBulldozerUnitPricesByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice, ExcavatorAndBulldozerUnitPriceDto>
{
    public ExcavatorAndBulldozerUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(e => e.AssignmentCode)
            .Include(e => e.ProductionProcess)
            .Include(e => e.Details).ThenInclude(d => d.HaulDistance)
            .Where(e =>
                string.IsNullOrWhiteSpace(searchTerm) ||
                (e.AssignmentCode != null && e.AssignmentCode.Name.ToLower().Contains(searchTerm)));

        Query.Select(e => new ExcavatorAndBulldozerUnitPriceDto
        {
            Id = e.Id,
            AssignmentCodeId = e.AssignmentCodeId,
            AssignmentCodeName = e.AssignmentCode != null ? e.AssignmentCode.Name : string.Empty,
            EquipmentQuality = e.EquipmentQuality,
            ProductionProcessId = e.ProductionProcessId,
            ProductionProcessName = e.ProductionProcess != null ? e.ProductionProcess.Name : string.Empty,
            StartMonth = e.StartMonth,
            EndMonth = e.EndMonth,
            Details = e.Details.Select(d => new ExcavatorAndBulldozerUnitPriceDetailDto
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
