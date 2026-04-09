using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Part.Queries.Part;

public record GetAllPartQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, DateTime Date, PartType? PartType) : IRequest<PaginationResponse<PartDto>>;

public class GetAllPartQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllPartQuery, PaginationResponse<PartDto>>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();

    public async Task<PaginationResponse<PartDto>> Handle(GetAllPartQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination,
        };

        var checkDate = new DateOnly(request.Date.Year, request.Date.Month, 1);
        var searchTerm = request.Search?.Trim().ToLower() ?? string.Empty;

        var parts = await _partRepository.GetAllAsync(
            predicate: p =>
                (!request.PartType.HasValue || p.Type == request.PartType.Value) &&
                (string.IsNullOrWhiteSpace(searchTerm)
                    || p.Name.ToLower().Contains(searchTerm)
                    || (p.Code != null && p.Code.Value.ToLower().Contains(searchTerm))
                    || p.EquipmentParts.Any(ep => ep.Equipment != null
                        && ep.Equipment.Code != null
                        && ep.Equipment.Code.Value.ToLower().Contains(searchTerm))),
            include: p => p
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.Code)
                .Include(p => p.Costs)
                .Include(p => p.EquipmentParts).ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code),
            disableTracking: true);

        var mapped = parts
            .Select(p => new PartDto
            {
                Id = p.Id,
                Code = p.Code?.Value ?? string.Empty,
                Name = p.Name,
                UnitOfMeasureId = p.UnitOfMeasureId,
                UnitOfMeasureName = p.UnitOfMeasure?.Name ?? string.Empty,
                PartType = p.Type,
                EquipmentIds = p.EquipmentParts.Select(ep => ep.EquipmentId).Distinct().ToList(),
                EquipmentCodes = p.EquipmentParts
                    .Where(ep => ep.Equipment?.Code != null)
                    .Select(ep => ep.Equipment!.Code!.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(code => code)
                    .ToList(),
                ReplacementTimeStandard = p.ReplacementTimeStandard,
                CostAmount = p.Costs
                    .Where(c => c.CostType == CostType.Part && c.StartMonth <= checkDate && c.EndMonth >= checkDate)
                    .Select(c => c.Amount)
                    .FirstOrDefault(),
                ActualAmount = p.Costs
                    .Where(c => c.CostType == CostType.Part && c.StartMonth <= checkDate && c.EndMonth >= checkDate)
                    .Select(c => c.ActualAmount)
                    .FirstOrDefault(),
            })
            .OrderBy(p => p.Code)
            .ThenBy(p => p.Name)
            .ToList();

        var totalCount = mapped.Count;
        var pagedData = filter.IgnorePagination
            ? mapped
            : mapped
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

        return new PaginationResponse<PartDto>(pagedData, totalCount, filter.PageNumber, filter.PageSize);
    }
}
