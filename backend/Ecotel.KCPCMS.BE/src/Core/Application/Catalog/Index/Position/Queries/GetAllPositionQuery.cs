using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Catalog.Index.Position.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Repositories;
using Application.Common.Services;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Position;
using MediatR;

namespace Application.Catalog.Index.Position.Queries;

public record GetAllPositionQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<PositionDto>>;

public class GetAllPositionQueryHandler(
    IPaginationService paginationService,
    IReadRepository<Domain.Entities.Index.Position> positionRepository)
    : IRequestHandler<GetAllPositionQuery, PaginationResponse<PositionDto>>
{
    public async Task<PaginationResponse<PositionDto>> Handle(GetAllPositionQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new PositionsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: positionRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(p => p.Name).ToList();
        return result;
    }
}
