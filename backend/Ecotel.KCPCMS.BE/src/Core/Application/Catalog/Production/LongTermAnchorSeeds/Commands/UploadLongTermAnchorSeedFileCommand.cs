using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Production.LongTermAnchorSeeds.Commands;

public record UploadLongTermAnchorSeedFileCommand(IFormFile File, Guid DepartmentId) : IRequest<bool>;

public class UploadLongTermAnchorSeedFileCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<UploadLongTermAnchorSeedFileCommand, bool>
{
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<LongTermAnchorSeed> _seedRepository = unitOfWork.GetRepository<LongTermAnchorSeed>();
    private readonly IWriteRepository<LongTermAnchorSeedItem> _seedItemRepository = unitOfWork.GetRepository<LongTermAnchorSeedItem>();
    private readonly IWriteRepository<LongTermAnchorSeedProcessGroupMetric> _processGroupMetricRepository = unitOfWork.GetRepository<LongTermAnchorSeedProcessGroupMetric>();

    public async Task<bool> Handle(UploadLongTermAnchorSeedFileCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException(CustomResponseMessage.FileEmpty);
        }

        var departmentExists = await _departmentRepository.ExistsAsync(x => x.Id == request.DepartmentId);
        if (!departmentExists)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        using var stream = request.File.OpenReadStream();
        var rows = excelService.ImportFromExcel<LongTermAnchorSeedExcelRowDto>(stream) ?? [];
        if (rows.Count == 0)
        {
            throw new BadRequestException("File mốc gốc không có dữ liệu");
        }

        var materialCodes = rows
            .Select(x => ExtractCode(x.MaterialCode, x.PartCode))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
        var processGroupCodes = rows
            .Select(x => ExtractCode(x.ProcessGroupCode))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var parts = await _partRepository.GetAllAsync(
            predicate: x => x.Code != null && materialCodes.Contains(x.Code.Value),
            include: q => q.Include(x => x.Code),
            disableTracking: true);
        var processGroups = await _processGroupRepository.GetAllAsync(
            predicate: x => x.Code != null && processGroupCodes.Contains(x.Code.Value),
            include: q => q.Include(x => x.Code),
            disableTracking: true);

        var missingMaterialCodes = materialCodes.Except(parts.Select(x => x.Code?.Value ?? string.Empty)).ToList();
        if (missingMaterialCodes.Count > 0)
        {
            throw new BadRequestException($"Không tìm thấy vật tư với mã: {string.Join(", ", missingMaterialCodes)}.");
        }

        var missingProcessGroupCodes = processGroupCodes.Except(processGroups.Select(x => x.Code?.Value ?? string.Empty)).ToList();
        if (missingProcessGroupCodes.Count > 0)
        {
            throw new BadRequestException($"Không tìm thấy nhóm công đoạn với mã: {string.Join(", ", missingProcessGroupCodes)}.");
        }

        var seed = await _seedRepository.GetFirstOrDefaultAsync(
            predicate: s => s.DepartmentId == request.DepartmentId,
            include: q => q
                .Include(s => s.Items)
                .Include(s => s.ProcessGroupMetrics),
            disableTracking: false);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (seed == null)
            {
                seed = LongTermAnchorSeed.Create(request.DepartmentId);
                await _seedRepository.InsertAsync(seed, cancellationToken);
                await unitOfWork.SaveChangesAsync();
            }

            var rowIds = rows.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).Distinct().ToList();
            var existingItems = rowIds.Count > 0
                ? await _seedItemRepository.GetAllAsync(
                    predicate: i => i.LongTermAnchorSeedId == seed.Id && rowIds.Contains(i.Id),
                    disableTracking: false)
                : [];

            var seedItemsByCompositeKey = (await _seedItemRepository.GetAllAsync(
                predicate: i => i.LongTermAnchorSeedId == seed.Id,
                disableTracking: false))
                .ToDictionary(x => (x.MaterialId, x.ProcessGroupId));

            foreach (var (row, index) in rows.Select((row, index) => (row, index)))
            {
                var isDeleteRow = IsDeleteRow(row);
                LongTermAnchorSeedItem? existingItem = null;
                if (row.Id.HasValue)
                {
                    existingItem = existingItems.FirstOrDefault(x => x.Id == row.Id.Value);
                    if (existingItem == null)
                    {
                        throw new BadRequestException($"Không tìm thấy dòng mốc gốc với id '{row.Id}'.");
                    }
                }

                if (isDeleteRow)
                {
                    if (existingItem != null)
                    {
                        _seedItemRepository.Delete(existingItem);
                        seedItemsByCompositeKey.Remove((existingItem.MaterialId, existingItem.ProcessGroupId));
                    }

                    continue;
                }

                var materialCode = ExtractCode(row.MaterialCode, row.PartCode);
                var processGroupCode = ExtractCode(row.ProcessGroupCode);
                if (string.IsNullOrWhiteSpace(materialCode) || string.IsNullOrWhiteSpace(processGroupCode))
                {
                    throw new BadRequestException("Mã vật tư và mã nhóm công đoạn không được để trống");
                }

                var part = parts.FirstOrDefault(x => x.Code?.Value == materialCode)
                    ?? throw new BadRequestException($"Không tìm thấy vật tư với mã '{materialCode}'.");
                var processGroup = processGroups.FirstOrDefault(x => x.Code?.Value == processGroupCode)
                    ?? throw new BadRequestException($"Không tìm thấy nhóm công đoạn với mã '{processGroupCode}'.");

                ValidateBusinessRule(row, materialCode, processGroupCode);

                if (existingItem != null)
                {
                    if (existingItem.MaterialId != part.Id || existingItem.ProcessGroupId != processGroup.Id)
                    {
                        throw new BadRequestException($"Dòng '{row.Id}' không khớp vật tư/nhóm công đoạn hiện tại.");
                    }
                }
                else
                {
                    seedItemsByCompositeKey.TryGetValue((part.Id, processGroup.Id), out existingItem);
                }

                if (existingItem == null)
                {
                    var newItem = LongTermAnchorSeedItem.Create(
                        seed.Id,
                        processGroup.Id,
                        part.Id,
                        index,
                        row.IssuedQuantity ?? 0,
                        row.UnitPrice ?? 0,
                        row.PendingValueStartPeriod ?? 0,
                        row.UsageTime ?? 0,
                        row.AllocatedTime ?? 0,
                        row.AllocationRatio ?? 0,
                        row.Note,
                        null);

                    await _seedItemRepository.InsertAsync(newItem, cancellationToken);
                    seedItemsByCompositeKey[(part.Id, processGroup.Id)] = newItem;
                    continue;
                }

                existingItem.Update(
                    processGroup.Id,
                    part.Id,
                    index,
                    row.IssuedQuantity ?? 0,
                    row.UnitPrice ?? 0,
                    row.PendingValueStartPeriod ?? 0,
                    row.UsageTime ?? 0,
                    row.AllocatedTime ?? 0,
                    row.AllocationRatio ?? 0,
                    row.Note);

                _seedItemRepository.Update(existingItem);
            }

            var metricByProcessGroup = new Dictionary<Guid, (double PlannedOutput, double StandardOutput)>();
            foreach (var row in rows)
            {
                var processGroupCode = ExtractCode(row.ProcessGroupCode);
                if (string.IsNullOrWhiteSpace(processGroupCode))
                {
                    continue;
                }

                if (!row.PlannedOutput.HasValue && !row.StandardOutput.HasValue)
                {
                    continue;
                }

                var processGroup = processGroups.First(x => x.Code?.Value == processGroupCode);
                var currentMetric = (
                    PlannedOutput: row.PlannedOutput ?? 0,
                    StandardOutput: row.StandardOutput ?? 0);

                if (metricByProcessGroup.TryGetValue(processGroup.Id, out var existingMetric)
                    && (existingMetric.PlannedOutput != currentMetric.PlannedOutput
                        || existingMetric.StandardOutput != currentMetric.StandardOutput))
                {
                    throw new BadRequestException($"Nhóm công đoạn '{processGroupCode}' có sản lượng kế hoạch/định mức không nhất quán giữa các dòng.");
                }

                metricByProcessGroup[processGroup.Id] = currentMetric;
            }

            var existingMetrics = await _processGroupMetricRepository.GetAllAsync(
                predicate: x => x.LongTermAnchorSeedId == seed.Id,
                disableTracking: false);

            foreach (var metric in metricByProcessGroup)
            {
                var existingMetric = existingMetrics.FirstOrDefault(x => x.ProcessGroupId == metric.Key);
                if (existingMetric == null)
                {
                    var newMetric = LongTermAnchorSeedProcessGroupMetric.Create(
                        seed.Id,
                        metric.Key,
                        metric.Value.PlannedOutput,
                        metric.Value.StandardOutput);
                    await _processGroupMetricRepository.InsertAsync(newMetric, cancellationToken);
                    continue;
                }

                existingMetric.Update(metric.Value.PlannedOutput, metric.Value.StandardOutput);
                _processGroupMetricRepository.Update(existingMetric);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string ExtractCode(string? primaryValue, string? fallbackValue = null)
    {
        var value = !string.IsNullOrWhiteSpace(primaryValue) ? primaryValue : fallbackValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Split(" - ", 2, StringSplitOptions.TrimEntries)[0].Trim();
    }

    private static bool IsDeleteRow(LongTermAnchorSeedExcelRowDto row)
    {
        return row.Id.HasValue
            && string.IsNullOrWhiteSpace(row.MaterialCode)
            && string.IsNullOrWhiteSpace(row.PartCode)
            && string.IsNullOrWhiteSpace(row.ProcessGroupCode)
            && !row.IssuedQuantity.HasValue
            && !row.UnitPrice.HasValue
            && !row.PendingValueStartPeriod.HasValue
            && !row.UsageTime.HasValue
            && !row.AllocatedTime.HasValue
            && !row.AllocationRatio.HasValue
            && string.IsNullOrWhiteSpace(row.Note);
    }

    private static void ValidateBusinessRule(LongTermAnchorSeedExcelRowDto row, string materialCode, string processGroupCode)
    {
        var hasPending = row.PendingValueStartPeriod.HasValue && row.PendingValueStartPeriod.Value > 0;
        var hasIssuedQuantity = row.IssuedQuantity.HasValue && row.IssuedQuantity.Value > 0;
        var hasUnitPrice = row.UnitPrice.HasValue && row.UnitPrice.Value > 0;

        if (hasPending && (hasIssuedQuantity || hasUnitPrice))
        {
            throw new BadRequestException(
                $"Dòng vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}' không được nhập đồng thời giá trị đầu kỳ với số lượng hoặc đơn giá.");
        }

        if (!hasPending && hasIssuedQuantity != hasUnitPrice)
        {
            throw new BadRequestException(
                $"Dòng vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}' phải nhập đồng thời số lượng và đơn giá.");
        }

        if (!hasPending && !hasIssuedQuantity && !hasUnitPrice)
        {
            throw new BadRequestException(
                $"Dòng vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}' phải nhập giá trị đầu kỳ hoặc đồng thời số lượng và đơn giá.");
        }
    }
}
