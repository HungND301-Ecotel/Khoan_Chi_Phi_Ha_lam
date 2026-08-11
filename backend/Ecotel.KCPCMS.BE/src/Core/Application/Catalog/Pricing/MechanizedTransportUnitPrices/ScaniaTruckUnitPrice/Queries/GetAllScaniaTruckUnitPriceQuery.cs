using Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Queries;

public record GetAllScaniaTruckUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ScaniaTruckUnitPriceDto>>;

public class GetAllScaniaTruckUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice> repository) : IRequestHandler<GetAllScaniaTruckUnitPriceQuery, PaginationResponse<ScaniaTruckUnitPriceDto>>
{
    public async Task<PaginationResponse<ScaniaTruckUnitPriceDto>> Handle(GetAllScaniaTruckUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };
        var spec = new ScaniaTruckUnitPricesByPaginationSpec(filter, request.Search);

        var rawList = await paginationService.PaginatedListAsync(
            repository: repository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        rawList.Data = rawList.Data.OrderBy(d => d.StartMonth).ToList();
        return rawList;
    }
}