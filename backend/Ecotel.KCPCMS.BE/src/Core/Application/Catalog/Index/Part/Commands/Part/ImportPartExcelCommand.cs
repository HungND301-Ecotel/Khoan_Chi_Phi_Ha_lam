using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Index.Part.Commands.Part;

public record ImportPartExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportPartExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService, ICodeService codeService) : IRequestHandler<ImportPartExcelCommand, bool>
{
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<PartProcessGroup> _partProcessGroupRepository = unitOfWork.GetRepository<PartProcessGroup>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(ImportPartExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<PartExcelDto>(stream) ?? [];

        var importErrors = new List<string>();
        var importRows = new List<PartImportRow>();

        for (var i = 0; i < dtos.Count; i++)
        {
            if (TryBuildImportRow(dtos[i], i + 2, importRows, importErrors))
            {
                continue;
            }
        }

        var groupedRows = importRows
            .GroupBy(r => r.PartCodeNormalized)
            .ToList();

        foreach (var rowGroup in groupedRows)
        {
            ValidateConsistentPartRows(rowGroup, importErrors);
        }

        var equipmentCodes = importRows
            .Select(r => r.EquipmentCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitNames = importRows
            .Select(r => r.UnitName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var processGroupCodes = importRows
            .SelectMany(r => r.ProcessGroupCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => unitNames.Contains(p.Name),
            disableTracking: false);
        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var equipmentEntities = await _equipmentRepository.GetAllAsync(
            predicate: p => p.Code != null && equipmentCodes.Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: false);
        var equipmentMap = equipmentEntities.ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
        var equipmentCodeSet = equipmentMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var processGroupEntities = await _processGroupRepository.GetAllAsync(
            predicate: p => p.Code != null && processGroupCodes.Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: false);
        var processGroupMap = processGroupEntities.ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
        var processGroupCodeSet = processGroupMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dbUnitOfMeasureNames = unitOfMeasureIdMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in importRows)
        {
            if (string.IsNullOrWhiteSpace(row.UnitName) || !dbUnitOfMeasureNames.Contains(row.UnitName))
            {
                importErrors.Add($"Mã phụ tùng '{row.PartCodeDisplay}' có đơn vị tính '{row.Dto.UnitOfMeasureName}' không tồn tại ở dòng {row.RowNumber}.");
            }

            if (!equipmentCodeSet.Contains(row.EquipmentCode))
            {
                importErrors.Add($"Mã phụ tùng '{row.PartCodeDisplay}' có mã thiết bị '{row.Dto.EquipmentCode}' không tồn tại ở dòng {row.RowNumber}.");
            }

            var invalidProcessGroupCodes = row.ProcessGroupCodes
                .Where(code => !processGroupCodeSet.Contains(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (invalidProcessGroupCodes.Any())
            {
                importErrors.Add($"Mã phụ tùng '{row.PartCodeDisplay}' có mã nhóm công đoạn sản xuất '{string.Join(", ", invalidProcessGroupCodes)}' không tồn tại ở dòng {row.RowNumber}.");
            }
        }

        var excelDtos = new List<(PartEntity Entity, int RowNumber, string PartCode, List<Cost> Costs, List<Guid> ProcessGroupIds)>();
        foreach (var rowGroup in groupedRows)
        {
            var firstRow = rowGroup.First();
            var rowNumber = firstRow.RowNumber;
            var dto = firstRow.Dto;

            if (!unitOfMeasureIdMap.TryGetValue(firstRow.UnitName, out var unitOfMeasureId))
            {
                importErrors.Add($"Mã phụ tùng '{firstRow.PartCodeDisplay}' có đơn vị tính '{dto.UnitOfMeasureName}' không tồn tại ở dòng {rowNumber}.");
                continue;
            }

            var equipmentCodesByPart = rowGroup
                .Select(row => NormalizeEquipmentCode(row.EquipmentCode))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var mappedEquipments = equipmentCodesByPart
                .Select(code => equipmentMap.GetValueOrDefault(code))
                .Where(e => e != null)
                .Select(e => e!)
                .ToList();
            var mappedProcessGroupIds = firstRow.ProcessGroupCodes
                .Select(code => processGroupMap.GetValueOrDefault(code)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (!mappedEquipments.Any())
            {
                importErrors.Add($"Mã phụ tùng '{firstRow.PartCodeDisplay}' không có mã thiết bị hợp lệ ở dòng {rowNumber}.");
                continue;
            }
            if (!mappedProcessGroupIds.Any())
            {
                importErrors.Add($"Mã phụ tùng '{firstRow.PartCodeDisplay}' không có mã nhóm công đoạn sản xuất hợp lệ ở dòng {rowNumber}.");
                continue;
            }

            var partId = Guid.NewGuid();
            var partEntity = PartEntity.Create(partId, dto.Code, dto.Name, unitOfMeasureId, dto.ReplacementTimeStandard, mappedEquipments, PartType.Part);
            try
            {
                var costList = costService.ParseExcelCostString(dto.Cost, CostType.Part, partId);
                costList = RebuildPartCosts(costList, partId);

                if (!TryValidateSinglePartCosts(partEntity.Code?.Value ?? firstRow.PartCodeDisplay, costList, partId, out var singleCostError))
                {
                    importErrors.Add(singleCostError);
                    continue;
                }

                excelDtos.Add((partEntity, rowNumber, firstRow.PartCodeNormalized, costList, mappedProcessGroupIds));
            }
            catch (Exception ex)
            {
                importErrors.Add($"Mã phụ tùng '{firstRow.PartCodeDisplay}' có đơn giá '{dto.Cost}' không hợp lệ ở dòng {rowNumber}. Chi tiết: {ex.Message}");
            }
        }

        var dbParts = await _partRepository.GetAllAsync(
            predicate: p => p.Type == PartType.Part,
            include: p => p
                .Include(p => p.Code)
                .Include(p => p.Costs)
                .Include(p => p.PartProcessGroups).ThenInclude(ppg => ppg.ProcessGroup).ThenInclude(pg => pg.Code)
                .Include(p => p.EquipmentParts).ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code),
            disableTracking: false);

        var deleteList = new List<PartEntity>();
        var deleteCost = new List<Cost>();
        var insertCost = new List<Cost>();
        var deletePartProcessGroups = new List<PartProcessGroup>();
        var insertPartProcessGroups = new List<PartProcessGroup>();
        var updateList = new List<PartEntity>();
        var addList = new List<PartEntity>();

        var excelPartCodes = excelDtos.Select(x => x.PartCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        deleteList.AddRange(dbParts.Where(x => !excelPartCodes.Contains(NormalizePartCode(x.Code?.Value))));
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

        var dbPartItems = dbParts
            .Where(p => p.Code != null && !string.IsNullOrWhiteSpace(p.Code.Value))
            .Select(p => new
            {
                Code = NormalizePartCode(p.Code!.Value),
                Part = p
            })
            .ToList();

        var excelRowsByPartCode = groupedRows.ToDictionary(
            g => g.Key,
            g => g.ToList(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var group in dbPartItems.GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            if (!excelRowsByPartCode.TryGetValue(group.Key, out var excelRowsForCode) || !excelRowsForCode.Any())
            {
                continue;
            }

            var dbEquipmentGroups = group
                .SelectMany(x => x.Part.EquipmentParts
                    .Where(ep => ep.Equipment?.Code != null)
                    .Select(ep => new
                    {
                        EquipmentCode = NormalizeEquipmentCode(ep.Equipment!.Code!.Value),
                        PartId = x.Part.Id
                    }))
                .GroupBy(x => x.EquipmentCode, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Select(v => v.PartId).Distinct().Count() > 1)
                .ToDictionary(g => g.Key, g => g.Select(v => v.PartId).Distinct().ToList(), StringComparer.OrdinalIgnoreCase);

            var conflictedExcelRows = excelRowsForCode
                .Where(r => dbEquipmentGroups.ContainsKey(NormalizeEquipmentCode(r.EquipmentCode)))
                .ToList();

            if (conflictedExcelRows.Any())
            {
                var conflictedLines = string.Join(", ", conflictedExcelRows.Select(r => r.RowNumber).OrderBy(x => x));
                var conflictedEquipments = string.Join("; ",
                    conflictedExcelRows
                        .GroupBy(r => NormalizeEquipmentCode(r.EquipmentCode), StringComparer.OrdinalIgnoreCase)
                        .Select(g =>
                        {
                            var partIds = string.Join(", ", dbEquipmentGroups[g.Key]);
                            var lines = string.Join(", ", g.Select(x => x.RowNumber).OrderBy(x => x));
                            return $"thiết bị '{g.Key}' (dòng {lines}, DB PartId: {partIds})";
                        }));

                importErrors.Add(
                    $"Mã phụ tùng '{group.Key}' bị trùng theo cả PartCode + EquipmentCode trong DB. " +
                    $"Các dòng Excel liên quan: {conflictedLines}. Chi tiết: {conflictedEquipments}.");
            }
        }

        var dbPartDict = dbPartItems
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(x => x.Part)
                    .OrderByDescending(p => p.LastModifiedOn ?? p.CreatedOn)
                    .ThenByDescending(p => p.CreatedOn)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var excelDto in excelDtos)
        {
            var dto = excelDto.Entity;
            var rowNumber = excelDto.RowNumber;
            var partCode = excelDto.PartCode;
            var parsedCosts = excelDto.Costs;
            var processGroupIds = excelDto.ProcessGroupIds;

            if (dbPartDict.TryGetValue(partCode, out var entityToUpdate))
            {
                bool isInfoChanged = entityToUpdate.CheckChange(dto);
                bool isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), parsedCosts);
                var existingProcessGroupIds = entityToUpdate.PartProcessGroups
                    .Select(ppg => ppg.ProcessGroupId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();
                var incomingProcessGroupIds = processGroupIds
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();
                var isProcessGroupChanged = !existingProcessGroupIds.SequenceEqual(incomingProcessGroupIds);

                if (isInfoChanged || isCostChanged || isProcessGroupChanged)
                {
                    entityToUpdate.Update(
                        dto.Code!.Value,
                        dto.Name,
                        dto.UnitOfMeasureId,
                        dto.ReplacementTimeStandard,
                        dto.EquipmentParts.Select(ep => ep.Equipment).Where(e => e != null).ToList()!,
                        PartType.Part);

                    if (isCostChanged)
                    {
                        deleteCost.AddRange(entityToUpdate.Costs.ToList());
                        insertCost.AddRange(RebuildPartCosts(parsedCosts, entityToUpdate.Id));
                    }
                    if (isProcessGroupChanged)
                    {
                        deletePartProcessGroups.AddRange(entityToUpdate.PartProcessGroups.ToList());
                        insertPartProcessGroups.AddRange(incomingProcessGroupIds
                            .Select(processGroupId => PartProcessGroup.Create(entityToUpdate.Id, processGroupId)));
                    }

                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsPartCodeExisted(dto.Code!.Value))
                {
                    importErrors.Add($"Mã phụ tùng '{dto.Code!.Value}' đã tồn tại ở dòng {rowNumber}.");
                    continue;
                }

                addList.Add(dto);
                insertCost.AddRange(RebuildPartCosts(parsedCosts, dto.Id));
                insertPartProcessGroups.AddRange(processGroupIds
                    .Distinct()
                    .Select(processGroupId => PartProcessGroup.Create(dto.Id, processGroupId)));
            }
        }

        if (!TryValidatePartCosts(insertCost, out var partCostError))
        {
            importErrors.Add(partCostError);
        }

        ThrowIfImportErrors(importErrors);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _partRepository.Delete(deleteList);

                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
            }

            if (addList.Any())
            {
                await _partRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                if (deleteCost.Any())
                {
                    _costRepository.Delete(deleteCost);
                }
                if (deletePartProcessGroups.Any())
                {
                    _partProcessGroupRepository.Delete(deletePartProcessGroups);
                }

                _partRepository.Update(updateList);
            }

            if (insertCost.Any())
            {
                await _costRepository.InsertAsync(insertCost);
            }
            if (insertPartProcessGroups.Any())
            {
                await _partProcessGroupRepository.InsertAsync(insertPartProcessGroups);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken: cancellationToken);
            throw;
        }
    }

    private static void ThrowIfImportErrors(List<string> importErrors)
    {
        var errors = importErrors
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (errors.Count == 0)
        {
            return;
        }

        throw new ExcelImportException(errors);
    }

    private static bool TryBuildImportRow(PartExcelDto dto, int rowNumber, ICollection<PartImportRow> rows, ICollection<string> errors)
    {
        var partCodeDisplay = (dto.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(partCodeDisplay))
        {
            errors.Add($"Thiếu mã phụ tùng ở dòng {rowNumber}.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add($"Mã phụ tùng '{partCodeDisplay}' thiếu tên phụ tùng ở dòng {rowNumber}.");
            return false;
        }

        var equipmentCode = ExtractEquipmentCodeFromDisplay(dto.EquipmentCode);
        var processGroupCodes = ParseProcessGroupCodes(dto.ProcessGroupCodes);
        if (!processGroupCodes.Any())
        {
            errors.Add($"Mã phụ tùng '{partCodeDisplay}' thiếu mã nhóm công đoạn sản xuất ở dòng {rowNumber}.");
            return false;
        }

        rows.Add(new PartImportRow(
            dto,
            rowNumber,
            NormalizePartCode(partCodeDisplay),
            partCodeDisplay,
            dto.Name.Trim(),
            dto.UnitOfMeasureName?.Trim() ?? string.Empty,
            equipmentCode,
            processGroupCodes,
            NormalizeProcessGroupCodes(processGroupCodes),
            NormalizeCostString(dto.Cost)));

        return true;
    }
    private static string ExtractEquipmentCodeFromDisplay(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var separatorIndex = normalized.IndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex <= 0 ? normalized : normalized[..separatorIndex].Trim();
    }

    private static List<string> ParseProcessGroupCodes(string? processGroupCodes)
    {
        return (processGroupCodes ?? string.Empty)
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(code => code.Trim().ToUpperInvariant())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToList();
    }

    private static string NormalizeProcessGroupCodes(IEnumerable<string> processGroupCodes)
    {
        return string.Join(",",
            processGroupCodes
                .Select(code => code.Trim().ToUpperInvariant())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code));
    }

    private static void ValidateConsistentPartRows(IGrouping<string, PartImportRow> rowGroup, ICollection<string> errors)
    {
        var rows = rowGroup.OrderBy(x => x.RowNumber).ToList();
        if (rows.Count <= 1)
        {
            return;
        }

        var duplicateEquipmentGroups = rows
            .GroupBy(r => NormalizeEquipmentCode(r.EquipmentCode), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicateEquipmentGroups.Any())
        {
            var duplicateDetails = string.Join("; ", duplicateEquipmentGroups.Select(g =>
                $"thiết bị '{g.Key}' ở các dòng {string.Join(", ", g.Select(x => x.RowNumber))}"));
            errors.Add($"Mã phụ tùng '{rows[0].PartCodeDisplay}' bị trùng cặp Mã phụ tùng + Mã thiết bị: {duplicateDetails}.");
        }

        var first = rows[0];
        foreach (var row in rows.Skip(1))
        {
            if (!string.Equals(first.PartName, row.PartName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(first.UnitName, row.UnitName, StringComparison.OrdinalIgnoreCase)
                || first.Dto.ReplacementTimeStandard != row.Dto.ReplacementTimeStandard
                || !string.Equals(first.ProcessGroupCodesNormalized, row.ProcessGroupCodesNormalized, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(first.CostNormalized, row.CostNormalized, StringComparison.OrdinalIgnoreCase))
            {
                var lineList = string.Join(", ", rows.Select(r => r.RowNumber));
                errors.Add(
                    $"Mã phụ tùng '{first.PartCodeDisplay}' có dữ liệu không đồng nhất ở các dòng {lineList}. " +
                    "Các thông tin phải giống nhau: Tên phụ tùng, Đơn vị tính, Định mức thời gian thay thế, Mã nhóm công đoạn sản xuất, Đơn giá. " +
                    "Chỉ được phép khác Mã thiết bị/Tên thiết bị.");
                break;
            }
        }
    }

    private static string NormalizePartCode(string? partCode) => (partCode ?? string.Empty).Trim().ToUpperInvariant();
    private static string NormalizeCostString(string? cost) => (cost ?? string.Empty).Replace(" ", string.Empty).Trim();
    private static string NormalizeEquipmentCode(string? equipmentCode) => (equipmentCode ?? string.Empty).Trim();

    private static List<Cost> RebuildPartCosts(IEnumerable<Cost> costs, Guid partId)
    {
        return costs
            .Select(c => Cost.Create(c.StartMonth, c.EndMonth, CostType.Part, c.Amount, partId, c.ActualAmount))
            .ToList();
    }

    private static bool TryValidateSinglePartCosts(string partCode, IEnumerable<Cost> costs, Guid partId, out string error)
    {
        foreach (var cost in costs)
        {
            var parentCount =
                (cost.PartId.HasValue ? 1 : 0) +
                (cost.MaterialId.HasValue ? 1 : 0) +
                (cost.EquipmentId.HasValue ? 1 : 0);

            if (parentCount != 1 || cost.PartId != partId)
            {
                error = $"Mã phụ tùng '{partCode}' có dữ liệu đơn giá không hợp lệ.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidatePartCosts(IEnumerable<Cost> costs, out string error)
    {
        foreach (var cost in costs)
        {
            var parentCount =
                (cost.PartId.HasValue ? 1 : 0) +
                (cost.MaterialId.HasValue ? 1 : 0) +
                (cost.EquipmentId.HasValue ? 1 : 0);

            if (parentCount != 1 || !cost.PartId.HasValue)
            {
                error = "Dữ liệu đơn giá phụ tùng không hợp lệ.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private sealed record PartImportRow(
        PartExcelDto Dto,
        int RowNumber,
        string PartCodeNormalized,
        string PartCodeDisplay,
        string PartName,
        string UnitName,
        string EquipmentCode,
        List<string> ProcessGroupCodes,
        string ProcessGroupCodesNormalized,
        string CostNormalized);
}
