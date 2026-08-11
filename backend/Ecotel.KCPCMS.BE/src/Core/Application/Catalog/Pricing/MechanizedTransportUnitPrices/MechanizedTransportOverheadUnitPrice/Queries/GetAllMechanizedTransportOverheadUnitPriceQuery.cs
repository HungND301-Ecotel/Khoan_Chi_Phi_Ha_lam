using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Queries;

public record GetAllMechanizedTransportOverheadUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<MechanizedTransportOverheadUnitPriceDto>>;

public class GetAllMechanizedTransportOverheadUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice> repository) : IRequestHandler<GetAllMechanizedTransportOverheadUnitPriceQuery, PaginationResponse<MechanizedTransportOverheadUnitPriceDto>>
{
    public async Task<PaginationResponse<MechanizedTransportOverheadUnitPriceDto>> Handle(GetAllMechanizedTransportOverheadUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };
        var spec = new MechanizedTransportOverheadUnitPricesByPaginationSpec(filter, request.Search);

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
