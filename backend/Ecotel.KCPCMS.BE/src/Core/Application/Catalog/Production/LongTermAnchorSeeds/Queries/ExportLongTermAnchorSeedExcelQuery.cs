using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Production.LongTermAnchorSeeds.Queries;

public record ExportLongTermAnchorSeedExcelQuery(Guid DepartmentId) : IRequest<ExportLongTermAnchorSeedExcelResponse>;

public record ExportLongTermAnchorSeedExcelResponse(byte[] FileBytes, string FileName);

public class ExportLongTermAnchorSeedExcelQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ExportLongTermAnchorSeedExcelQuery, ExportLongTermAnchorSeedExcelResponse>
{
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<LongTermAnchorSeed> _seedRepository = unitOfWork.GetRepository<LongTermAnchorSeed>();
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<ExportLongTermAnchorSeedExcelResponse> Handle(ExportLongTermAnchorSeedExcelQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string>
        {
            nameof(LongTermAnchorSeedExcelRowDto.Id)
        };

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
                    .ThenInclude(i => i.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
                .Include(s => s.ProcessGroupMetrics),
            disableTracking: true);

        var processGroupMetrics = seed?.ProcessGroupMetrics
            .GroupBy(x => x.ProcessGroupId)
            .ToDictionary(
                x => x.Key,
                x => (
                    PlannedOutput: x.First().PlannedOutput,
                    StandardOutput: x.First().StandardOutput)) ?? [];

        var rows = seed?.Items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ProcessGroup.Code != null ? x.ProcessGroup.Code.Value : string.Empty)
            .ThenBy(x => GetTrackedMaterialCode(x))
            .Select(x => new LongTermAnchorSeedExcelRowDto
            {
                Id = x.Id,
                MaterialCode = BuildCodeName(GetTrackedMaterialCode(x), GetTrackedMaterialName(x)),
                ProcessGroupCode = BuildCodeName(x.ProcessGroup.Code?.Value, x.ProcessGroup.Name),
                IssuedQuantity = x.IssuedQuantity,
                UnitPrice = x.UnitPrice,
                PendingValueStartPeriod = x.PendingValueStartPeriod,
                UsageTime = x.UsageTime,
                AllocatedTime = x.AllocatedTime,
                AllocationRatio = x.AllocationRatio,
                PlannedOutput = processGroupMetrics.TryGetValue(x.ProcessGroupId, out var metric) ? metric.PlannedOutput : null,
                StandardOutput = processGroupMetrics.TryGetValue(x.ProcessGroupId, out metric) ? metric.StandardOutput : null,
                Note = x.Note
            })
            .ToList() ?? [];

        var parts = await _partRepository.GetAllAsync(
            predicate: x => x.Code != null,
            include: q => q.Include(x => x.Code),
            disableTracking: true);
        var processGroups = await _processGroupRepository.GetAllAsync(
            predicate: x => x.Code != null,
            include: q => q.Include(x => x.Code),
            disableTracking: true);

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            {
                nameof(LongTermAnchorSeedExcelRowDto.MaterialCode),
                parts
                    .Select(x => BuildCodeName(GetMaterialCode(x), GetMaterialName(x)))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            },
            {
                nameof(LongTermAnchorSeedExcelRowDto.PartCode),
                parts
                    .Select(x => BuildCodeName(GetMaterialCode(x), GetMaterialName(x)))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            },
            {
                nameof(LongTermAnchorSeedExcelRowDto.ProcessGroupCode),
                processGroups
                    .Select(x => BuildCodeName(x.Code?.Value, x.Name))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            }
        };

        var fileName = $"long-term-anchor-seed-{department.Code?.Value?.ToLowerInvariant() ?? department.Id.ToString("N")}.xlsx";
        var fileBytes = excelService.ExportToExcel(rows, "AnchorSeed", hiddenProperties, dropdownData: dropdownConfigs);
        return new ExportLongTermAnchorSeedExcelResponse(fileBytes, fileName);
    }

    private static string BuildCodeName(string? code, string? name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(name) ? code : $"{code} - {name}";
    }

    private static string GetMaterialCode(PartEntity part)
        => part.Code?.Value ?? string.Empty;

    private static string GetMaterialName(PartEntity part)
        => part.Name;

    private static string GetTrackedMaterialCode(LongTermAnchorSeedItem item)
        => item.Material?.Code?.Value ?? item.Part?.Code?.Value ?? string.Empty;

    private static string GetTrackedMaterialName(LongTermAnchorSeedItem item)
        => item.Material?.Name ?? item.Part?.Name ?? string.Empty;

}
