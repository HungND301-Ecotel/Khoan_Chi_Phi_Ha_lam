using Application.Catalog.Pricing.MaterialUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Common.Enums;
using MediatR;
using DomainEntities = Domain.Entities.Pricing.MaterialUnitPrice;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Queries;

public record class GetAllMaterialUnitPricesUnifiedQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, MaterialUnitPriceType? Type) : IRequest<PaginationResponse<AllMaterialUnitPricesDto>>;

public class GetAllMaterialUnitPricesUnifiedQueryHandler(
    IPaginationService paginationService,
    IReadRepository<DomainEntities.MaterialUnitPrice> materialUnitPriceRepository)
    : IRequestHandler<GetAllMaterialUnitPricesUnifiedQuery, PaginationResponse<AllMaterialUnitPricesDto>>
{
    public async Task<PaginationResponse<AllMaterialUnitPricesDto>> Handle(GetAllMaterialUnitPricesUnifiedQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new AllMaterialUnitPricesByPaginationSpec(filter, request.Search, request.Type);

        var result = await paginationService.PaginatedListAsync(
            repository: materialUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderByCodeNatural(d => d.Code).ThenBy(d => d.ProcessName).ToList();
        return result;
    }
}
