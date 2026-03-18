using Application.Catalog.Pricing.LongwallMaterialUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Queries;

public record class GetAllLongwallMaterialUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<LongwallMaterialUnitPriceDto>>;

public class GetAllLongwallMaterialUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice> materialUnitPriceRepository)
    : IRequestHandler<GetAllLongwallMaterialUnitPriceQuery, PaginationResponse<LongwallMaterialUnitPriceDto>>
{
    public async Task<PaginationResponse<LongwallMaterialUnitPriceDto>> Handle(GetAllLongwallMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new LongwallMaterialUnitPricesByPaginationSpec(filter, request.Search);

        return await paginationService.PaginatedListAsync(
            repository: materialUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}
