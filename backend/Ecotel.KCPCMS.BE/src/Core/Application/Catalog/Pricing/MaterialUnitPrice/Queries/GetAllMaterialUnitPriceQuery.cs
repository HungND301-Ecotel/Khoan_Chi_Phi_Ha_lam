using Application.Catalog.Pricing.MaterialUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Queries;
public record class GetAllMaterialUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<MaterialUnitPriceDto>>;

public class GetAllUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<TunnelExcavationMaterialUnitPrice> maintainUnitPriceRepository)
    : IRequestHandler<GetAllMaterialUnitPriceQuery, PaginationResponse<MaterialUnitPriceDto>>
{
    public async Task<PaginationResponse<MaterialUnitPriceDto>> Handle(GetAllMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new MaterialUnitPricesByPaginationSpec(filter, request.Search);

        return await paginationService.PaginatedListAsync(
            repository: maintainUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}
