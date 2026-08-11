using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Queries;

public record GetAllExcavatorAndBulldozerUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ExcavatorAndBulldozerUnitPriceDto>>;

public class GetAllExcavatorAndBulldozerUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> repository) : IRequestHandler<GetAllExcavatorAndBulldozerUnitPriceQuery, PaginationResponse<ExcavatorAndBulldozerUnitPriceDto>>
{
    public async Task<PaginationResponse<ExcavatorAndBulldozerUnitPriceDto>> Handle(GetAllExcavatorAndBulldozerUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };
        var spec = new ExcavatorAndBulldozerUnitPricesByPaginationSpec(filter, request.Search);

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
