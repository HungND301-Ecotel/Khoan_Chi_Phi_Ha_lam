// File: Application/Catalog/Passport/Specifications/PassportsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Passport;
using Ardalis.Specification;

namespace Application.Catalog.Index.Passport.Specifications;

public class PassportsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.Passport, PassportDto>
{
    public PassportsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Name.ToLower().Contains(searchTerm));
        Query
            .Select(p => new PassportDto
            {
                Id = p.Id,
                Name = p.Name,
                Sd = p.Sd,
                Sc = p.Sc,
                CreateOn = p.CreatedOn
            });
    }
}