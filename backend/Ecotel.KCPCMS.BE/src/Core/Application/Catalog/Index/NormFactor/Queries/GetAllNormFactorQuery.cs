using Application.Catalog.Index.AdjustmentFactor.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.AssignmentCode;
using Application.Dto.Catalog.NormFactor;
using MediatR;

namespace Application.Catalog.Index.AdjustmentFactor.Queries;

public record class GetAllNormFactorQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<NormFactorDto>>;

public class GetAllNormFactorQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.NormFactor> normFactorRepository) : IRequestHandler<GetAllNormFactorQuery, PaginationResponse<NormFactorDto>>
{
    public async Task<PaginationResponse<NormFactorDto>> Handle(GetAllNormFactorQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new NormFactorsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: normFactorRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        foreach (var item in result.Data)
        {
            item.AffectAssignmentCodes = item.AssignmentCodes
                .Select(a => new ShortAssignmentCodeDto
                {
                    Id = a.AssignmentCodeId,
                    Code = a.AssignmentCode,
                    Name = a.AssignmentCodeName
                })
                .DistinctBy(x => x.Id)
                .ToList();
        }

        result.Data = result.Data
            .OrderByCodeNatural(d => d.ProcessGroupCode)
            .ThenBy(d => d.HardnessName)
            .ThenBy(d => d.StoneClampRatioName)
            .ToList();

        return result;
    }
}