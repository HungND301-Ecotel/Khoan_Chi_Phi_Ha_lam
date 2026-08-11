using Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Queries;

public record GetAllWasteSuctionTruckUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<WasteSuctionTruckUnitPriceDto>>;

public class GetAllWasteSuctionTruckUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice> repository) : IRequestHandler<GetAllWasteSuctionTruckUnitPriceQuery, PaginationResponse<WasteSuctionTruckUnitPriceDto>>
{
    public async Task<PaginationResponse<WasteSuctionTruckUnitPriceDto>> Handle(GetAllWasteSuctionTruckUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };
        var spec = new WasteSuctionTruckUnitPricesByPaginationSpec(filter, request.Search);

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