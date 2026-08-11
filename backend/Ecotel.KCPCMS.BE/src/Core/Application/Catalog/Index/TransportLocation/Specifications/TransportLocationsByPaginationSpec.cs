using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.TransportLocation;
using Microsoft.EntityFrameworkCore;
using Ardalis.Specification;

namespace Application.Catalog.Index.TransportLocation.Specifications;

public class TransportLocationsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.TransportLocation, TransportLocationDto>
{
    public TransportLocationsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(t => t.Code)
            .Where(t =>
                t.Code != null &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                 t.Name.ToLower().Contains(searchTerm) ||
                 t.Code.Value.ToLower().Contains(searchTerm)));

        Query.Select(t => new TransportLocationDto
        {
            Id = t.Id,
            Code = t.Code.Value,
            Name = t.Name,
            Note = t.Note,
            LocationType = t.LocationType
        });
    }
}