using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Ardalis.Specification;


namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Specifications;

public class MechanizedTransportOverheadUnitPricesByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice, MechanizedTransportOverheadUnitPriceDto>
{
    public MechanizedTransportOverheadUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(m => m.ProcessGroup)
            .Where(m =>
                string.IsNullOrWhiteSpace(searchTerm) ||
                (m.ProcessGroup != null && m.ProcessGroup.Name.ToLower().Contains(searchTerm)));

        Query.Select(m => new MechanizedTransportOverheadUnitPriceDto
        {
            Id = m.Id,
            ProcessGroupId = m.ProcessGroupId,
            ProcessGroupName = m.ProcessGroup != null ? m.ProcessGroup.Name : string.Empty,
            StartMonth = m.StartMonth,
            EndMonth = m.EndMonth,
            LowValuePerishableSupplyUnitPrice = m.LowValuePerishableSupplyUnitPrice,
            ElectricityUnitPrice = m.ElectricityUnitPrice
        });
    }
}