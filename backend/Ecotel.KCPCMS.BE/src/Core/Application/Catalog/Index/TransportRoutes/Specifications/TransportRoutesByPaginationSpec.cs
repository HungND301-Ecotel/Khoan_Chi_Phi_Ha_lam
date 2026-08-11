using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.TransportRoute;
using Domain.Entities.Index;
using Ardalis.Specification;

namespace Application.Catalog.Index.TransportRoutes.Specifications;

public class TransportRoutesByPaginationSpec : EntitiesByPaginationFilterSpec<TransportRoute, TransportRouteDto>
{
    public TransportRoutesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(t => string.IsNullOrWhiteSpace(searchTerm) ||
                        t.Name.ToLower().Contains(searchTerm) ||
                        (t.Code != null && t.Code.Value.ToLower().Contains(searchTerm)));
        Query
            .Select(t => new TransportRouteDto
            {
                Id = t.Id,
                Code = t.Code!.Value,
                Name = t.Name,
                Note = t.Note,
                IsSpecialLowVolume = t.IsSpecialLowVolume
            });
    }
}