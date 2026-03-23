using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.Services;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Part.Queries.Part;

public record GetAllPartQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, DateTime Date) : IRequest<PaginationResponse<PartDto>>;

public class GetAllPartQueryHandler(IPaginationService paginationService, IUnitOfWork unitOfWork) : IRequestHandler<GetAllPartQuery, PaginationResponse<PartDto>>
{
    IWriteRepository<Domain.Entities.Index.Equipment> equipmentRepository = unitOfWork.GetRepository<Domain.Entities.Index.Equipment>();
    public async Task<PaginationResponse<PartDto>> Handle(GetAllPartQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination,
        };

        var checkDate = new DateOnly(request.Date.Year, request.Date.Month, 1);

        var equipments = await equipmentRepository.GetAllAsync(
            predicate: e =>
                string.IsNullOrWhiteSpace(request.Search) ||
                e.Name.ToLower().Contains(request.Search) ||
                e.EquipmentParts.Any(ep =>
                    ep.Part.Code != null &&
                    ep.Part.Code.Value.ToLower().Contains(request.Search)),
            include: e => e.Include(e => e.Code)
                .Include(e => e.EquipmentParts)
                    .ThenInclude(ep => ep.Part)
                    .ThenInclude(p => p.UnitOfMeasure)
                .Include(e => e.EquipmentParts)
                    .ThenInclude(ep => ep.Part)
                    .ThenInclude(p => p.Code)
                .Include(e => e.EquipmentParts)
                    .ThenInclude(ep => ep.Part)
                    .ThenInclude(p => p.Costs),
            disableTracking: true
            );

        var parts = equipments
            .SelectMany(e => e.EquipmentParts, (e, ep) => new PartDto
            {
                Id = ep.Part.Id,
                EquipmentPartId = ep.Id,
                Code = ep.Part.Code?.Value ?? string.Empty,
                EquipmentCode = e.Code?.Value ?? string.Empty,
                EquipmentId = e.Id,
                Name = ep.Part.Name,
                UnitOfMeasureId = ep.Part.UnitOfMeasureId,
                UnitOfMeasureName = ep.Part.UnitOfMeasure?.Name ?? string.Empty,
                ReplacementTimeStandard = ep.Part.ReplacementTimeStandard,
                CostAmount = ep.Part.Costs
                    .Where(c => c.CostType == CostType.Part &&
                                c.StartMonth <= checkDate &&
                                c.EndMonth >= checkDate)
                    .Select(c => c.Amount)
                    .FirstOrDefault()
            })
            .OrderBy(p => p.EquipmentCode)
            .ThenBy(p => p.Name)
            .ToList();

        // Áp dụng pagination thủ công sau khi flatten
        var totalCount = parts.Count;
        var pagedData = filter.IgnorePagination
            ? parts
            : parts
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

        return new PaginationResponse<PartDto>(pagedData, totalCount, filter.PageNumber, filter.PageSize);
    }
}
