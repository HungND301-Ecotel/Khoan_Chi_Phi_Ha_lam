using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.CargoType;
using Ardalis.Specification;

namespace Application.Catalog.Index.CargoType.Specifications;

public class CargoTypeByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.CargoType, CargoTypeDto>
{
    public CargoTypeByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();
        Query
            .Include(h => h.Code)
            .Where(h =>
                h.Code != null &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                 h.Name.ToLower().Contains(searchTerm) ||
                 h.Code.Value.ToLower().Contains(searchTerm))
            );
        Query.Select(h => new CargoTypeDto  
        {
            Id = h.Id,
            Code = h.Code.Value,
            Name = h.Name,
            Note = h.Note
        });
    }
}
