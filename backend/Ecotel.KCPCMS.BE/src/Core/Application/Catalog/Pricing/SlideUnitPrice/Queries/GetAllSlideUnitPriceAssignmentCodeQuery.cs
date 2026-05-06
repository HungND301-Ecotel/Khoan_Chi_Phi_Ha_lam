using Application.Catalog.Pricing.SlideUnitPrice.Specifications;
using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.SlideUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.SlideUnitPrice.Queries;

public record class GetAllSlideUnitPriceAssignmentCodeQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<SlideUnitPriceAssignmentCodeDto>>;

public class GetAllSlideUnitPriceAssignmentCodeQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.SlideUnitPriceAssignmentCode> slideUnitPriceAssignmentCoceRepository, ICacheService cacheService) : IRequestHandler<GetAllSlideUnitPriceAssignmentCodeQuery, PaginationResponse<SlideUnitPriceAssignmentCodeDto>>
{
    private const string CacheSignalKey = "SlideUnitPrice";

    public async Task<PaginationResponse<SlideUnitPriceAssignmentCodeDto>> Handle(GetAllSlideUnitPriceAssignmentCodeQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"GetAllSlideUnitPriceAssignmentCode:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}";
        var cachedResult = await cacheService.GetAsync<PaginationResponse<SlideUnitPriceAssignmentCodeDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new SlideUnitPriceAssignmentCodesByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: slideUnitPriceAssignmentCoceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data
            .OrderBy(d => d.ProcessGroupId)
            .ThenBy(d => d.PassportId)
            .ThenBy(d => d.HardnessId)
            .ThenBy(d => d.MaterialId)
            .ToList();
        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);
        return result;
    }
}
