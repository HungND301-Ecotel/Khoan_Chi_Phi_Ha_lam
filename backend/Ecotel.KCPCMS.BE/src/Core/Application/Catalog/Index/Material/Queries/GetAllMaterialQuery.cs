using Application.Catalog.Index.Material.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.Material;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;

namespace Application.Catalog.Index.Material.Queries;

public record GetAllMaterialQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, MaterialType? MaterialType, DateTime Date) : IRequest<PaginationResponse<MaterialDto>>;

public class GetAllMaterialQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.Material> materialRepository) : IRequestHandler<GetAllMaterialQuery, PaginationResponse<MaterialDto>>
{
    public async Task<PaginationResponse<MaterialDto>> Handle(GetAllMaterialQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination,
            OrderBy = [$"{nameof(Domain.Entities.Index.Material.Name)}"]
        };
        var spec = new MaterialsByPaginationSpec(filter, request.Search, request.MaterialType, request.Date);

        var rawList = await paginationService.PaginatedListAsync(
            repository: materialRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
        rawList.Data = rawList.Data.OrderByCodeNatural(d => d.AssignmentCode).ThenBy(d => d.Name).ToList();
        return rawList;
    }
}
