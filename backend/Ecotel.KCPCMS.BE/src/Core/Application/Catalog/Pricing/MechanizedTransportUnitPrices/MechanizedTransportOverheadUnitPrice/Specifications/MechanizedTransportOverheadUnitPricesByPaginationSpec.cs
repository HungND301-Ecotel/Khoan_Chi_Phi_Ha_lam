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
            .Include(m => m.Department)
            .Where(m =>
                string.IsNullOrWhiteSpace(searchTerm) ||
                (m.ProcessGroup != null && (m.ProcessGroup.Name.ToLower().Contains(searchTerm) || (m.ProcessGroup.Code != null && m.ProcessGroup.Code.Value.ToLower().Contains(searchTerm)))) ||
                (m.Department != null && (m.Department.Name.ToLower().Contains(searchTerm) || (m.Department.Code != null && m.Department.Code.Value.ToLower().Contains(searchTerm)))));

        Query.Select(m => new MechanizedTransportOverheadUnitPriceDto
        {
            Id = m.Id,
            ProcessGroupId = m.ProcessGroupId,
            ProcessGroupCode = m.ProcessGroup != null && m.ProcessGroup.Code != null ? m.ProcessGroup.Code.Value : string.Empty,
            ProcessGroupName = m.ProcessGroup != null ? m.ProcessGroup.Name : string.Empty,
            DepartmentId = m.DepartmentId,
            DepartmentCode = m.Department != null && m.Department.Code != null ? m.Department.Code.Value : string.Empty,
            DepartmentName = m.Department != null ? m.Department.Name : string.Empty,
            StartMonth = m.StartMonth,
            EndMonth = m.EndMonth,
            LowValuePerishableSupplyUnitPrice = m.LowValuePerishableSupplyUnitPrice,
            ElectricityUnitPrice = m.ElectricityUnitPrice
        });
    }
}