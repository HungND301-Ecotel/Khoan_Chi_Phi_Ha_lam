using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.LongTermAnchorSeeds.Queries;

public record GetLongTermAnchorSeedDetailQuery(Guid DepartmentId) : IRequest<GetLongTermAnchorSeedDetailResponseDto>;

public class GetLongTermAnchorSeedDetailQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLongTermAnchorSeedDetailQuery, GetLongTermAnchorSeedDetailResponseDto>
{
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<LongTermAnchorSeed> _seedRepository = unitOfWork.GetRepository<LongTermAnchorSeed>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<GetLongTermAnchorSeedDetailResponseDto> Handle(GetLongTermAnchorSeedDetailQuery request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetFirstOrDefaultAsync(
            predicate: d => d.Id == request.DepartmentId,
            include: q => q.Include(d => d.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var seed = await _seedRepository.GetFirstOrDefaultAsync(
            predicate: s => s.DepartmentId == request.DepartmentId,
            include: q => q
                .Include(s => s.Items)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(p => p.Code)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(p => p.UnitOfMeasure)
                .Include(s => s.Items)
                    .ThenInclude(i => i.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
                .Include(s => s.ProcessGroupMetrics)
                    .ThenInclude(m => m.ProcessGroup)
                        .ThenInclude(pg => pg.Code),
            disableTracking: true);

        var reports = await _acceptanceReportRepository.GetAllAsync(
            predicate: a => a.ProductionOutput.DepartmentId == request.DepartmentId,
            include: q => q.Include(a => a.ProductionOutput),
            disableTracking: true);

        var processGroupMetrics = seed?.Items
            .Select(item => item.ProcessGroup)
            .Where(processGroup => processGroup != null)
            .GroupBy(processGroup => processGroup.Id)
            .Select(group =>
            {
                var existingMetric = seed.ProcessGroupMetrics.FirstOrDefault(metric => metric.ProcessGroupId == group.Key);
                var processGroup = group.First();

                return new LongTermAnchorSeedProcessGroupMetricDto
                {
                    Id = existingMetric?.Id ?? Guid.Empty,
                    ProcessGroupId = group.Key,
                    ProcessGroupCode = processGroup.Code?.Value ?? string.Empty,
                    ProcessGroupName = processGroup.Name,
                    PlannedOutput = existingMetric?.PlannedOutput ?? 0,
                    StandardOutput = existingMetric?.StandardOutput ?? 0
                };
            })
            .OrderBy(x => x.ProcessGroupCode)
            .ToList() ?? [];

        return new GetLongTermAnchorSeedDetailResponseDto
        {
            DepartmentId = department.Id,
            DepartmentCode = department.Code?.Value ?? string.Empty,
            DepartmentName = department.Name,
            EffectiveMonth = LongTermAnchorSeedTrackingHelper.ResolveEffectiveMonth(
                reports.Select(x => x.ProductionOutput?.StartMonth ?? default).Where(x => x != default)),
            ProcessGroupMetrics = processGroupMetrics,
            Items = seed?.Items
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.ProcessGroup.Code != null ? x.ProcessGroup.Code.Value : string.Empty)
                .ThenBy(x => x.Part.Code != null ? x.Part.Code.Value : string.Empty)
                .Select(item => new LongTermAnchorSeedItemDto
                {
                    Id = item.Id,
                    PartId = item.PartId,
                    ProcessGroupId = item.ProcessGroupId,
                    PartCode = item.Part.Code?.Value ?? string.Empty,
                    PartName = item.Part.Name,
                    UnitOfMeasureName = item.Part.UnitOfMeasure?.Name ?? string.Empty,
                    ProcessGroupCode = item.ProcessGroup.Code?.Value ?? string.Empty,
                    ProcessGroupName = item.ProcessGroup.Name,
                    IssuedQuantity = item.IssuedQuantity,
                    UnitPrice = item.UnitPrice,
                    PendingValueStartPeriod = item.PendingValueStartPeriod,
                    UsageTime = item.UsageTime,
                    AllocatedTime = item.AllocatedTime,
                    RemainingTime = item.RemainingTime,
                    AllocationRatio = item.AllocationRatio,
                    OriginAmount = item.OriginAmount,
                    TotalAmount = item.TotalAmount,
                    TotalValueToAccount = item.TotalValueToAccount,
                    Note = item.Note
                })
                .ToList() ?? []
        };
    }
}
