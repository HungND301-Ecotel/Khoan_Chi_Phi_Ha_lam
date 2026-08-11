using Application.Catalog.Pricing.MechanizedTransportUnitPrices.ServiceAndCraneVehicleUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ServiceAndCraneVehicleUnitPrice.Queries;

public record GetAllServiceAndCraneVehicleUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ServiceAndCraneVehicleUnitPriceDto>>;

public class GetAllServiceAndCraneVehicleUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice> repository) : IRequestHandler<GetAllServiceAndCraneVehicleUnitPriceQuery, PaginationResponse<ServiceAndCraneVehicleUnitPriceDto>>
{
    public async Task<PaginationResponse<ServiceAndCraneVehicleUnitPriceDto>> Handle(GetAllServiceAndCraneVehicleUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };
        var spec = new ServiceAndCraneVehicleUnitPricesByPaginationSpec(filter, request.Search);

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