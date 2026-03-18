using Application.Catalog.Pricing.SlideUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.SlideUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.SlideUnitPrice.Queries;
public record class GetAllSlideUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<SlideUnitPriceDto>>;

public class GetAllUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.SlideUnitPrice> slideUnitPriceRepository) : IRequestHandler<GetAllSlideUnitPriceQuery, PaginationResponse<SlideUnitPriceDto>>
{
    public async Task<PaginationResponse<SlideUnitPriceDto>> Handle(GetAllSlideUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new SlideUnitPricesByPaginationSpec(filter, request.Search);

        return await paginationService.PaginatedListAsync(
            repository: slideUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}
