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

namespace Application.Catalog.Production.LongTermAnchorSeeds.Commands;

public record UploadLongTermAnchorSeedFileCommand(IFormFile File, Guid DepartmentId) : IRequest<bool>;

public class UploadLongTermAnchorSeedFileCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<UploadLongTermAnchorSeedFileCommand, bool>
{
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<Material> _materialRepository = unitOfWork.GetRepository<Material>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<ProductionOrder>();
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

        var importErrors = new List<string>();

        var materialCodes = rows
            .Select(x => NormalizeCode(ExtractCode(x.MaterialCode)))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var processGroupCodes = rows
            .Select(x => NormalizeCode(ExtractCode(x.ProcessGroupCode)))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var assignmentCodes = rows
            .Select(x => NormalizeCode(ExtractCode(x.CategoryAssignmentCode)))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var productionOrderCodes = rows
            .Select(x => NormalizeCode(ExtractCode(x.CategoryProductionOrderCode)))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var materials = await _materialRepository.GetAllAsync(
            predicate: x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value),
            include: q => q.Include(x => x.Code),
            disableTracking: true);
        var processGroups = await _processGroupRepository.GetAllAsync(
            predicate: x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value),
            include: q => q.Include(x => x.Code),
            disableTracking: true);
        var assignmentCodeEntities = await _assignmentCodeRepository.GetAllAsync(
            predicate: x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value),
            include: q => q.Include(x => x.Code),
            disableTracking: true);
        var productionOrders = await _productionOrderRepository.GetAllAsync(
            predicate: x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value),
            include: q => q.Include(x => x.Code),
            disableTracking: true);

        var materialByCode = materials
            .Where(x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value))
            .GroupBy(x => NormalizeCode(x.Code!.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var processGroupByCode = processGroups
            .Where(x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value))
            .GroupBy(x => NormalizeCode(x.Code!.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var assignmentCodeByCode = assignmentCodeEntities
            .Where(x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value))
            .GroupBy(x => NormalizeCode(x.Code!.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var productionOrderByCode = productionOrders
            .Where(x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value))
            .GroupBy(x => NormalizeCode(x.Code!.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var missingMaterialCodes = materialCodes
            .Where(code => !materialByCode.ContainsKey(code))
            .ToList();
        if (missingMaterialCodes.Count > 0)
        {
            importErrors.Add($"Không tìm thấy vật tư với mã: {string.Join(", ", missingMaterialCodes)}.");
        }

        var missingProcessGroupCodes = processGroupCodes
            .Where(code => !processGroupByCode.ContainsKey(code))
            .ToList();
        if (missingProcessGroupCodes.Count > 0)
        {
            importErrors.Add($"Không tìm thấy nhóm công đoạn với mã: {string.Join(", ", missingProcessGroupCodes)}.");
        }

        var missingAssignmentCodes = assignmentCodes
            .Where(code => !assignmentCodeByCode.ContainsKey(code))
            .ToList();
        if (missingAssignmentCodes.Count > 0)
        {
            importErrors.Add($"Không tìm thấy nhóm vật tư, tài sản với mã: {string.Join(", ", missingAssignmentCodes)}.");
        }

        var missingProductionOrderCodes = productionOrderCodes
            .Where(code => !productionOrderByCode.ContainsKey(code))
            .ToList();
        if (missingProductionOrderCodes.Count > 0)
        {
            importErrors.Add($"Không tìm thấy lệnh sản xuất với mã: {string.Join(", ", missingProductionOrderCodes)}.");
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

            var seedItems = await _seedItemRepository.GetAllAsync(
                predicate: i => i.LongTermAnchorSeedId == seed.Id,
                disableTracking: false);
            var existingItemsById = seedItems.ToDictionary(x => x.Id);

            var retainedExistingItemIds = rows
                .Where(x => x.Id.HasValue && !IsDeleteRow(x))
                .Select(x => x.Id!.Value)
                .Distinct()
                .ToHashSet();

            var allocationRatioByExistingItemId = seedItems
                .ToDictionary(x => x.Id, x => x.AllocationRatio);

            var missingRowIds = retainedExistingItemIds
                .Where(id => !existingItemsById.ContainsKey(id))
                .ToList();
            if (missingRowIds.Count > 0)
            {
                importErrors.AddRange(missingRowIds.Select(id => $"Không tìm thấy dòng mốc gốc với id '{id}'."));
            }

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var rowNumber = index + 2;
                if (IsDeleteRow(row))
                {
                    continue;
                }

                var materialCode = NormalizeCode(ExtractCode(row.MaterialCode));
                var processGroupCode = NormalizeCode(ExtractCode(row.ProcessGroupCode));
                var categoryAssignmentCode = NormalizeCode(ExtractCode(row.CategoryAssignmentCode));
                var categoryProductionOrderCode = NormalizeCode(ExtractCode(row.CategoryProductionOrderCode));

                if (string.IsNullOrWhiteSpace(materialCode) || string.IsNullOrWhiteSpace(processGroupCode))
                {
                    importErrors.Add($"Dòng {rowNumber}: Mã vật tư và mã nhóm công đoạn không được để trống.");
                    continue;
                }

                if (!materialByCode.ContainsKey(materialCode))
                {
                    importErrors.Add($"Dòng {rowNumber}: Không tìm thấy vật tư với mã '{materialCode}'.");
                }

                if (!processGroupByCode.ContainsKey(processGroupCode))
                {
                    importErrors.Add($"Dòng {rowNumber}: Không tìm thấy nhóm công đoạn với mã '{processGroupCode}'.");
                }

                if (!string.IsNullOrWhiteSpace(categoryAssignmentCode) && !assignmentCodeByCode.ContainsKey(categoryAssignmentCode))
                {
                    importErrors.Add($"Dòng {rowNumber}: Không tìm thấy nhóm vật tư, tài sản với mã '{categoryAssignmentCode}'.");
                }

                if (!string.IsNullOrWhiteSpace(categoryProductionOrderCode) && !productionOrderByCode.ContainsKey(categoryProductionOrderCode))
                {
                    importErrors.Add($"Dòng {rowNumber}: Không tìm thấy lệnh sản xuất với mã '{categoryProductionOrderCode}'.");
                }

                var businessRuleError = ValidateRowBusinessRule(row, materialCode, processGroupCode, rowNumber);
                if (!string.IsNullOrWhiteSpace(businessRuleError))
                {
                    importErrors.Add(businessRuleError);
                }
            }

            var metricValidationErrors = ValidateProcessGroupMetrics(rows);
            if (metricValidationErrors.Count > 0)
            {
                importErrors.AddRange(metricValidationErrors);
            }

            ThrowIfImportErrors(importErrors);

            var itemsToDelete = seedItems
                .Where(x => !retainedExistingItemIds.Contains(x.Id))
                .ToList();
            if (itemsToDelete.Count > 0)
            {
                _seedItemRepository.Delete(itemsToDelete);
                await unitOfWork.SaveChangesAsync();

                foreach (var item in itemsToDelete)
                {
                    existingItemsById.Remove(item.Id);
                }
            }

            foreach (var (row, index) in rows.Select((row, index) => (row, index)))
            {
                var isDeleteRow = IsDeleteRow(row);
                LongTermAnchorSeedItem? existingItem = null;
                if (row.Id.HasValue)
                {
                    existingItemsById.TryGetValue(row.Id.Value, out existingItem);
                }

                if (isDeleteRow)
                {
                    continue;
                }

                var materialCode = NormalizeCode(ExtractCode(row.MaterialCode));
                var processGroupCode = NormalizeCode(ExtractCode(row.ProcessGroupCode));
                var categoryAssignmentCode = NormalizeCode(ExtractCode(row.CategoryAssignmentCode));
                var categoryProductionOrderCode = NormalizeCode(ExtractCode(row.CategoryProductionOrderCode));
                if (string.IsNullOrWhiteSpace(materialCode) || string.IsNullOrWhiteSpace(processGroupCode))
                {
                    throw new BadRequestException("Mã vật tư và mã nhóm công đoạn không được để trống");
                }

                var material = materialByCode.GetValueOrDefault(materialCode)
                    ?? throw new BadRequestException($"Không tìm thấy vật tư với mã '{materialCode}'.");
                var processGroup = processGroupByCode.GetValueOrDefault(processGroupCode)
                    ?? throw new BadRequestException($"Không tìm thấy nhóm công đoạn với mã '{processGroupCode}'.");
                var assignmentCode = string.IsNullOrWhiteSpace(categoryAssignmentCode)
                    ? null
                    : assignmentCodeByCode.GetValueOrDefault(categoryAssignmentCode)
                        ?? throw new BadRequestException($"Không tìm thấy nhóm vật tư, tài sản với mã '{categoryAssignmentCode}'.");
                var productionOrder = string.IsNullOrWhiteSpace(categoryProductionOrderCode)
                    ? null
                    : productionOrderByCode.GetValueOrDefault(categoryProductionOrderCode)
                        ?? throw new BadRequestException($"Không tìm thấy lệnh sản xuất với mã '{categoryProductionOrderCode}'.");

                var normalizedValues = NormalizeRowValues(row);

                if (existingItem == null)
                {
                    var newItem = LongTermAnchorSeedItem.CreateForMaterial(
                        seed.Id,
                        processGroup.Id,
                        material.Id,
                        assignmentCode?.Id,
                        productionOrder?.Id,
                        index,
                        normalizedValues.IssuedQuantity,
                        normalizedValues.UnitPrice,
                        normalizedValues.PendingValueStartPeriod,
                        normalizedValues.UsageTime,
                        normalizedValues.AllocatedTime,
                        0,
                        normalizedValues.Note,
                        null);

                    await _seedItemRepository.InsertAsync(newItem, cancellationToken);
                    continue;
                }

                existingItem.UpdateForMaterial(
                    processGroup.Id,
                    material.Id,
                    assignmentCode?.Id,
                    productionOrder?.Id,
                    index,
                    normalizedValues.IssuedQuantity,
                    normalizedValues.UnitPrice,
                    normalizedValues.PendingValueStartPeriod,
                    normalizedValues.UsageTime,
                    normalizedValues.AllocatedTime,
                    allocationRatioByExistingItemId.GetValueOrDefault(existingItem.Id, existingItem.AllocationRatio),
                    normalizedValues.Note);

                _seedItemRepository.Update(existingItem);
            }

            var metricByProcessGroup = new Dictionary<Guid, (double PlannedOutput, double StandardOutput)>();
            foreach (var row in rows)
            {
                var processGroupCode = NormalizeCode(ExtractCode(row.ProcessGroupCode));
                if (string.IsNullOrWhiteSpace(processGroupCode))
                {
                    continue;
                }

                if (!row.PlannedOutput.HasValue && !row.StandardOutput.HasValue)
                {
                    continue;
                }

                var processGroup = processGroupByCode[processGroupCode];
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

    private static string ExtractCode(string? primaryValue)
    {
        var value = !string.IsNullOrWhiteSpace(primaryValue) ? primaryValue : null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Split(" - ", 2, StringSplitOptions.TrimEntries)[0].Trim();
    }

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Trim()
            .Replace("\u00A0", " ")
            .ToUpperInvariant();

        return string.Join(' ', normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsDeleteRow(LongTermAnchorSeedExcelRowDto row)
    {
        return row.Id.HasValue
            && string.IsNullOrWhiteSpace(row.MaterialCode)
            && string.IsNullOrWhiteSpace(row.ProcessGroupCode)
            && string.IsNullOrWhiteSpace(row.CategoryAssignmentCode)
            && string.IsNullOrWhiteSpace(row.CategoryProductionOrderCode)
            && !row.IssuedQuantity.HasValue
            && !row.UnitPrice.HasValue
            && !row.PendingValueStartPeriod.HasValue
            && !row.UsageTime.HasValue
            && !row.AllocatedTime.HasValue
            && string.IsNullOrWhiteSpace(row.Note);
    }

    private static NormalizedRowValues NormalizeRowValues(LongTermAnchorSeedExcelRowDto row)
    {
        return new NormalizedRowValues(
            row.IssuedQuantity ?? 0,
            row.UnitPrice ?? 0,
            row.PendingValueStartPeriod ?? 0,
            row.UsageTime ?? 0,
            row.AllocatedTime ?? 0,
            row.Note ?? string.Empty);
    }

    private sealed record NormalizedRowValues(
        double IssuedQuantity,
        decimal UnitPrice,
        decimal PendingValueStartPeriod,
        double UsageTime,
        double AllocatedTime,
        string Note);

    private static string? ValidateRowBusinessRule(
        LongTermAnchorSeedExcelRowDto row,
        string materialCode,
        string processGroupCode,
        int rowNumber)
    {
        var issuedQuantity = row.IssuedQuantity ?? 0;
        var unitPrice = row.UnitPrice ?? 0;
        var pendingValueStartPeriod = row.PendingValueStartPeriod ?? 0;
        var usageTime = row.UsageTime ?? 0;
        var allocatedTime = row.AllocatedTime ?? 0;
        if (issuedQuantity < 0)
        {
            return $"Dòng {rowNumber}: Số lượng của vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}' không được âm.";
        }

        if (unitPrice < 0)
        {
            return $"Dòng {rowNumber}: Đơn giá của vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}' không được âm.";
        }

        if (pendingValueStartPeriod < 0)
        {
            return $"Dòng {rowNumber}: Giá trị chờ hạch toán đầu kỳ của vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}' không được âm.";
        }

        if (usageTime < 0)
        {
            return $"Dòng {rowNumber}: Thời gian sử dụng của vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}' không được âm.";
        }

        if (allocatedTime < 0)
        {
            return $"Dòng {rowNumber}: Thời gian đã phân bổ của vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}' không được âm.";
        }

        var hasPendingValueStartPeriod = pendingValueStartPeriod > 0;
        var hasIssuedQuantity = issuedQuantity > 0;
        var hasUnitPrice = unitPrice > 0;

        if (!hasPendingValueStartPeriod && hasIssuedQuantity != hasUnitPrice)
        {
            return $"Dòng {rowNumber}: Phải nhập đồng thời số lượng và đơn giá khi không nhập giá trị chờ hạch toán đầu kỳ cho vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}'.";
        }

        if (hasPendingValueStartPeriod && (hasIssuedQuantity || hasUnitPrice))
        {
            return $"Dòng {rowNumber}: Không được nhập đồng thời giá trị chờ hạch toán đầu kỳ với số lượng hoặc đơn giá cho vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}'.";
        }

        if (!hasPendingValueStartPeriod && !hasIssuedQuantity && !hasUnitPrice)
        {
            return $"Dòng {rowNumber}: Phải nhập giá trị chờ hạch toán đầu kỳ hoặc đồng thời số lượng và đơn giá cho vật tư '{materialCode}' và nhóm công đoạn '{processGroupCode}'.";
        }

        return null;
    }

    private static List<string> ValidateProcessGroupMetrics(IReadOnlyList<LongTermAnchorSeedExcelRowDto> rows)
    {
        var errors = new List<string>();
        var metricByProcessGroup = new Dictionary<string, (double PlannedOutput, double StandardOutput)>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var rowNumber = index + 2;
            var processGroupCode = NormalizeCode(ExtractCode(row.ProcessGroupCode));

            if (string.IsNullOrWhiteSpace(processGroupCode))
            {
                continue;
            }

            if (!row.PlannedOutput.HasValue && !row.StandardOutput.HasValue)
            {
                continue;
            }

            var currentMetric = (
                PlannedOutput: row.PlannedOutput ?? 0,
                StandardOutput: row.StandardOutput ?? 0);

            if (metricByProcessGroup.TryGetValue(processGroupCode, out var existingMetric)
                && (existingMetric.PlannedOutput != currentMetric.PlannedOutput
                    || existingMetric.StandardOutput != currentMetric.StandardOutput))
            {
                errors.Add($"Dòng {rowNumber}: Nhóm công đoạn '{processGroupCode}' có sản lượng kế hoạch/định mức không nhất quán giữa các dòng.");
                continue;
            }

            metricByProcessGroup[processGroupCode] = currentMetric;
        }

        return errors;
    }

    private static void ThrowIfImportErrors(List<string> importErrors)
    {
        var errors = importErrors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (errors.Count == 0)
        {
            return;
        }

        throw new ExcelImportException(errors);
    }
}
