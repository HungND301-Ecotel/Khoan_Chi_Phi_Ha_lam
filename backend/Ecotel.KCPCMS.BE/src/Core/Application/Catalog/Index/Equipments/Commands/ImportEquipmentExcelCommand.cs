using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Equipments.Commands;

public record ImportEquipmentExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportEquipmentExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService, ICodeService codeService) : IRequestHandler<ImportEquipmentExcelCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<EquipmentPart> _equipmentPartRepository = unitOfWork.GetRepository<EquipmentPart>();
    private readonly IWriteRepository<EquipmentProcessGroup> _equipmentProcessGroupRepository = unitOfWork.GetRepository<EquipmentProcessGroup>();
    private readonly IWriteRepository<Code> _codeRepository = unitOfWork.GetRepository<Code>();

    public async Task<bool> Handle(ImportEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<EquipmentExcelDto>(stream) ?? [];

        var importErrors = new List<string>();
        var normalizedRows = BuildNormalizedRows(dtos, importErrors);

        var groupedRows = normalizedRows
            .GroupBy(r => BuildEquipmentKey(r.EquipmentCodeNormalized, r.ProcessGroupCode), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitNames = normalizedRows
            .Select(r => r.UnitName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var processGroupCodes = normalizedRows
            .Select(r => r.ProcessGroupCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => unitNames.Contains(p.Name),
            disableTracking: true);
        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var processGroups = await _processGroupRepository.GetAllAsync(
            predicate: p => p.Code != null && processGroupCodes.Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: true);
        var processGroupMap = processGroups.ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        var parts = await _partRepository.GetAllAsync(
            predicate: p => p.Code != null,
            include: p => p.Include(p => p.Code!),
            disableTracking: true);
        var partCodeMap = parts
            .Where(p => p.Code != null && !string.IsNullOrWhiteSpace(p.Code.Value))
            .GroupBy(p => NormalizePartCode(p.Code!.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var rowGroup in groupedRows)
        {
            var firstRow = rowGroup.OrderBy(r => r.RowNumber).First();

            if (!string.IsNullOrWhiteSpace(firstRow.UnitName) && !unitOfMeasureIdMap.ContainsKey(firstRow.UnitName))
            {
                importErrors.Add($"Mã thiết bị '{firstRow.EquipmentCodeDisplay}' có đơn vị tính '{firstRow.UnitName}' không tồn tại ở dòng {firstRow.RowNumber}.");
            }

            if (!processGroupMap.ContainsKey(firstRow.ProcessGroupCode))
            {
                importErrors.Add($"Mã thiết bị '{firstRow.EquipmentCodeDisplay}' có công đoạn sản xuất '{firstRow.ProcessGroupDisplay}' không tồn tại.");
            }

            var invalidPartCodes = rowGroup
                .SelectMany(r => r.PartCodes)
                .Where(code => !string.IsNullOrWhiteSpace(code) && !partCodeMap.ContainsKey(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (invalidPartCodes.Any())
            {
                importErrors.Add($"Mã thiết bị '{firstRow.EquipmentCodeDisplay}' có phụ tùng '{string.Join(", ", invalidPartCodes)}' không tồn tại.");
            }
        }

        var excelDtos = new List<(Equipment Entity, int RowNumber, string EquipmentKey, Guid ProcessGroupId, List<Guid> PartIds, string CostString)>();

        foreach (var rowGroup in groupedRows)
        {
            var rows = rowGroup.OrderBy(r => r.RowNumber).ToList();
            var firstRow = rows.First();

            if (!processGroupMap.TryGetValue(firstRow.ProcessGroupCode, out var processGroup))
            {
                continue;
            }

            Guid? unitOfMeasureId = null;
            if (!string.IsNullOrWhiteSpace(firstRow.UnitName)
                && unitOfMeasureIdMap.TryGetValue(firstRow.UnitName, out var mappedUnitId))
            {
                unitOfMeasureId = mappedUnitId;
            }

            var mappedPartIds = rows
                .SelectMany(r => r.PartCodes)
                .Where(code => !string.IsNullOrWhiteSpace(code) && partCodeMap.ContainsKey(code))
                .Select(code => partCodeMap[code].Id)
                .Distinct()
                .ToList();

            var equipmentId = Guid.NewGuid();
            try
            {
                var equipmentEntity = Equipment.Create(equipmentId, firstRow.EquipmentCodeDisplay, firstRow.EquipmentName, unitOfMeasureId);
                var costList = costService.ParseExcelCostString(firstRow.CostString, CostType.Electricity, equipmentId);
                equipmentEntity.AddCost(costList);

                var equipmentKey = BuildEquipmentKey(firstRow.EquipmentCodeNormalized, processGroup.Id);
                excelDtos.Add((equipmentEntity, firstRow.RowNumber, equipmentKey, processGroup.Id, mappedPartIds, firstRow.CostString));
            }
            catch (Exception ex)
            {
                importErrors.Add($"Mã thiết bị '{firstRow.EquipmentCodeDisplay}' có đơn giá '{firstRow.CostString}' không hợp lệ ở dòng {firstRow.RowNumber}. Chi tiết: {ex.Message}");
            }
        }

        ThrowIfImportErrors(importErrors);

        var dbEquipments = await _equipmentRepository.GetAllAsync(
            include: p => p
                .Include(p => p.Code)
                .Include(p => p.Costs)
                .Include(p => p.EquipmentProcessGroups)
                .Include(p => p.EquipmentParts),
            disableTracking: false);

        var deleteList = new List<Equipment>();
        var deleteCost = new List<Cost>();
        var insertCostsForUpdate = new List<Cost>();
        var deleteEquipmentProcessGroups = new List<EquipmentProcessGroup>();
        var insertEquipmentProcessGroups = new List<EquipmentProcessGroup>();
        var deleteEquipmentParts = new List<EquipmentPart>();
        var insertEquipmentParts = new List<EquipmentPart>();
        var updateList = new List<Equipment>();
        var addList = new List<Equipment>();

        var excelEquipmentKeys = excelDtos.Select(x => x.EquipmentKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        deleteList.AddRange(dbEquipments.Where(x =>
        {
            var processGroupId = x.EquipmentProcessGroups.Select(epg => (Guid?)epg.ProcessGroupId).FirstOrDefault();
            if (!processGroupId.HasValue || x.Code == null)
            {
                return true;
            }

            var key = BuildEquipmentKey(NormalizeEquipmentCode(x.Code.Value), processGroupId.Value);
            return !excelEquipmentKeys.Contains(key);
        }));

        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

        var dbEquipmentDict = dbEquipments
            .Where(p => p.Code != null
                        && !string.IsNullOrWhiteSpace(p.Code.Value)
                        && p.EquipmentProcessGroups.Any())
            .GroupBy(p => BuildEquipmentKey(
                    NormalizeEquipmentCode(p.Code!.Value),
                    p.EquipmentProcessGroups.Select(epg => epg.ProcessGroupId).First()),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(p => p.LastModifiedOn ?? p.CreatedOn)
                    .ThenByDescending(p => p.CreatedOn)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var excelDto in excelDtos)
        {
            var dto = excelDto.Entity;
            var rowNumber = excelDto.RowNumber;
            var processGroupIdByRow = excelDto.ProcessGroupId;
            var partIdsByRow = excelDto.PartIds.Distinct().OrderBy(id => id).ToList();

            if (dbEquipmentDict.TryGetValue(excelDto.EquipmentKey, out var entityToUpdate))
            {
                var isInfoChanged = entityToUpdate.CheckChange(dto);
                var isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), dto.Costs.ToList());
                var existingPartIds = entityToUpdate.EquipmentParts.Select(ep => ep.PartId).Distinct().OrderBy(id => id).ToList();
                var isPartChanged = !existingPartIds.SequenceEqual(partIdsByRow);

                if (isInfoChanged || isCostChanged || isPartChanged)
                {
                    entityToUpdate.Update(dto.Code!.Value, dto.Name, dto.UnitOfMeasureId);

                    if (isCostChanged)
                    {
                        deleteCost.AddRange(entityToUpdate.Costs.ToList());
                        var rebuiltCosts = costService.ParseExcelCostString(excelDto.CostString, CostType.Electricity, entityToUpdate.Id);
                        insertCostsForUpdate.AddRange(rebuiltCosts);
                    }

                    if (isPartChanged)
                    {
                        deleteEquipmentParts.AddRange(entityToUpdate.EquipmentParts.ToList());
                        insertEquipmentParts.AddRange(partIdsByRow.Select(partId => EquipmentPart.Create(entityToUpdate.Id, partId)));
                    }

                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsEquipmentCodeExisted(dto.Code!.Value, processGroupIdByRow))
                {
                    importErrors.Add($"Mã thiết bị '{dto.Code!.Value}' đã tồn tại ở dòng {rowNumber}.");
                    continue;
                }

                addList.Add(dto);
                insertEquipmentProcessGroups.Add(EquipmentProcessGroup.Create(dto.Id, processGroupIdByRow));
                insertEquipmentParts.AddRange(partIdsByRow.Select(partId => EquipmentPart.Create(dto.Id, partId)));
            }
        }

        ThrowIfImportErrors(importErrors);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _equipmentRepository.Delete(deleteList);
                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
            }

            if (addList.Any())
            {
                await _equipmentRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                if (deleteCost.Any())
                {
                    _costRepository.Delete(deleteCost);
                }

                if (deleteEquipmentProcessGroups.Any())
                {
                    _equipmentProcessGroupRepository.Delete(deleteEquipmentProcessGroups);
                }

                if (deleteEquipmentParts.Any())
                {
                    _equipmentPartRepository.Delete(deleteEquipmentParts);
                }

                _equipmentRepository.Update(updateList);
            }

            if (insertCostsForUpdate.Any())
            {
                await _costRepository.InsertAsync(insertCostsForUpdate, cancellationToken);
            }

            if (insertEquipmentProcessGroups.Any())
            {
                await _equipmentProcessGroupRepository.InsertAsync(insertEquipmentProcessGroups, cancellationToken);
            }

            if (insertEquipmentParts.Any())
            {
                await _equipmentPartRepository.InsertAsync(insertEquipmentParts, cancellationToken);
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

    private static List<EquipmentImportRow> BuildNormalizedRows(IList<EquipmentExcelDto> dtos, ICollection<string> errors)
    {
        var rows = new List<EquipmentImportRow>();

        string? currentCode = null;
        string? currentName = null;
        string? currentProcessGroup = null;
        string currentUnit = string.Empty;
        string currentCost = string.Empty;

        for (var i = 0; i < dtos.Count; i++)
        {
            var rowNumber = i + 2;
            var dto = dtos[i];

            var codeInput = (dto.Code ?? string.Empty).Trim();
            var nameInput = (dto.Name ?? string.Empty).Trim();
            var processInput = (dto.ProcessGroup ?? string.Empty).Trim();
            var unitInput = (dto.UnitOfMeasureName ?? string.Empty).Trim();
            var costInput = (dto.Cost ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(codeInput))
            {
                if (string.IsNullOrWhiteSpace(nameInput))
                {
                    errors.Add($"Mã thiết bị '{codeInput}' thiếu tên thiết bị ở dòng {rowNumber}.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(processInput))
                {
                    errors.Add($"Mã thiết bị '{codeInput}' thiếu công đoạn sản xuất ở dòng {rowNumber}.");
                    continue;
                }

                currentCode = codeInput;
                currentName = nameInput;
                currentProcessGroup = processInput;
                currentUnit = unitInput;
                currentCost = costInput;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(currentCode))
                {
                    errors.Add($"Thiếu mã thiết bị ở dòng {rowNumber}.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(nameInput))
                {
                    currentName = nameInput;
                }

                if (!string.IsNullOrWhiteSpace(processInput))
                {
                    currentProcessGroup = processInput;
                }

                if (!string.IsNullOrWhiteSpace(unitInput))
                {
                    currentUnit = unitInput;
                }

                if (!string.IsNullOrWhiteSpace(costInput))
                {
                    currentCost = costInput;
                }
            }

            if (string.IsNullOrWhiteSpace(currentName))
            {
                errors.Add($"Mã thiết bị '{currentCode}' thiếu tên thiết bị ở dòng {rowNumber}.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(currentProcessGroup))
            {
                errors.Add($"Mã thiết bị '{currentCode}' thiếu công đoạn sản xuất ở dòng {rowNumber}.");
                continue;
            }

            var processGroupCode = ParseProcessGroupCode(currentProcessGroup);
            if (string.IsNullOrWhiteSpace(processGroupCode))
            {
                errors.Add($"Mã thiết bị '{currentCode}' có công đoạn sản xuất '{currentProcessGroup}' không hợp lệ ở dòng {rowNumber}.");
                continue;
            }

            rows.Add(new EquipmentImportRow(
                RowNumber: rowNumber,
                EquipmentCodeDisplay: currentCode,
                EquipmentCodeNormalized: NormalizeEquipmentCode(currentCode),
                EquipmentName: currentName,
                ProcessGroupDisplay: currentProcessGroup,
                ProcessGroupCode: processGroupCode,
                UnitName: currentUnit,
                CostString: currentCost,
                PartCodes: ParsePartCodes(dto.PartCode)));
        }

        return rows;
    }

    private static string ParseProcessGroupCode(string? processGroupValue)
    {
        var value = (processGroupValue ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var splitIndex = value.IndexOf(" - ", StringComparison.Ordinal);
        if (splitIndex > 0)
        {
            value = value[..splitIndex];
        }

        return value.Trim().ToUpperInvariant();
    }

    private static List<string> ParsePartCodes(string? partCodes)
    {
        var raw = (partCodes ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }
        return [NormalizePartCode(raw)];
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

    private static string BuildEquipmentKey(string normalizedCode, Guid processGroupId) => $"{normalizedCode}|{processGroupId:N}";
    private static string BuildEquipmentKey(string normalizedCode, string processGroupCode) => $"{normalizedCode}|{processGroupCode}";
    private static string NormalizeEquipmentCode(string? equipmentCode) => (equipmentCode ?? string.Empty).Trim().ToUpperInvariant();
    private static string NormalizePartCode(string? partCode)
    {
        var upper = (partCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(upper))
        {
            return string.Empty;
        }

        return string.Join(' ', upper.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed record EquipmentImportRow(
        int RowNumber,
        string EquipmentCodeDisplay,
        string EquipmentCodeNormalized,
        string EquipmentName,
        string ProcessGroupDisplay,
        string ProcessGroupCode,
        string UnitName,
        string CostString,
        List<string> PartCodes);
}
