using Application.Catalog.Pricing.MaintainUnitPriceEquipment.Specifications;
using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MaintainUnitPriceEquipment;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using MediatR;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Queries;

public record GetAllMaintainUnitPriceEquipmentQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, MaintainUnitPriceType? Type) : IRequest<PaginationResponse<ShortMaintainUnitPriceDto>>;

public class GetAllUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<MaintainUnitPrice> maintainUnitPriceRepository, ICacheService cacheService) : IRequestHandler<GetAllMaintainUnitPriceEquipmentQuery, PaginationResponse<ShortMaintainUnitPriceDto>>
{
    private const string CacheSignalKey = "MaintainUnitPriceEquipment";

    public async Task<PaginationResponse<ShortMaintainUnitPriceDto>> Handle(GetAllMaintainUnitPriceEquipmentQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"GetAllMaintainUnitPriceEquipment:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}:{request.Type}";
        var cachedResult = await cacheService.GetAsync<PaginationResponse<ShortMaintainUnitPriceDto>>(cacheKey, cancellationToken);
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

        var spec = new MaintainUnitPricesByPaginationSpec(filter, request.Search, request.Type);

        var paginationResponse = await paginationService.PaginatedListAsync(
            repository: maintainUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        var listData = paginationResponse.Data.Select(m => new ShortMaintainUnitPriceDto
        {
            Id = m.Id,
            EquipmentId = m.EquipmentId,
            EquipmentCode = m.Equipment!.Code!.Value,
            EquipmentName = m.Equipment!.Name,
            TotalPrice = m.GetMaintainTotalPrice(),
            StartMonth = m.StartMonth,
            OtherMaterialValue = m.OtherMaterialValue,
            EndMonth = m.EndMonth,
            Type = m.Type
        })
        .OrderByCodeNatural(d => d.EquipmentCode)
        .ThenBy(d => d.EquipmentName)
        .ToList();

        var result = new PaginationResponse<ShortMaintainUnitPriceDto>(listData, paginationResponse.TotalCount, paginationResponse.CurrentPage, paginationResponse.PageSize);
        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);

        return result;
    }
}
