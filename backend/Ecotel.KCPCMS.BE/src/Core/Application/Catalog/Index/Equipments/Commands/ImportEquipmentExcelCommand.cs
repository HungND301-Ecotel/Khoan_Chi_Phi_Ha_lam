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
    private readonly IWriteRepository<EquipmentProcessGroup> _equipmentProcessGroupRepository = unitOfWork.GetRepository<EquipmentProcessGroup>();
    private readonly IWriteRepository<Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(ImportEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<EquipmentExcelDto>(stream) ?? [];

        var importErrors = new List<string>();
        var importRows = new List<EquipmentImportRow>();

        for (var i = 0; i < dtos.Count; i++)
        {
            TryBuildImportRow(dtos[i], i + 2, importRows, importErrors);
        }

        var groupedRows = importRows
            .GroupBy(r => r.EquipmentCodeNormalized)
            .ToList();

        foreach (var rowGroup in groupedRows)
        {
            ValidateConsistentEquipmentRows(rowGroup, importErrors);
        }

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
        var dbUnitOfMeasureNames = unitOfMeasureIdMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var processGroups = await _processGroupRepository.GetAllAsync(
            predicate: p => p.Code != null && processGroupCodes.Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: false);
        var processGroupMap = processGroups.ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
        var processGroupCodeSet = processGroupMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in importRows)
        {
            if (!string.IsNullOrWhiteSpace(row.UnitName) && !dbUnitOfMeasureNames.Contains(row.UnitName))
            {
                importErrors.Add($"Mã thiết bị '{row.EquipmentCodeDisplay}' có đơn vị tính '{row.Dto.UnitOfMeasureName}' không tồn tại ở dòng {row.RowNumber}.");
            }

            var invalidProcessGroupCodes = row.ProcessGroupCodes
                .Where(code => !processGroupCodeSet.Contains(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (invalidProcessGroupCodes.Any())
            {
                importErrors.Add($"Mã thiết bị '{row.EquipmentCodeDisplay}' có mã nhóm công đoạn sản xuất '{string.Join(", ", invalidProcessGroupCodes)}' không tồn tại ở dòng {row.RowNumber}.");
            }
        }

        // Each item: (parsed cost string, unitOfMeasureId, processGroupIds, rowNumber, equipmentCodeNormalized, dto)
        var excelDtos = new List<(Equipment Entity, int RowNumber, string EquipmentCode, List<Guid> ProcessGroupIds, string CostString)>();

        foreach (var rowGroup in groupedRows)
        {
            var firstRow = rowGroup.First();
            var rowNumber = firstRow.RowNumber;
            var dto = firstRow.Dto;

            Guid? unitOfMeasureId = null;
            if (!string.IsNullOrWhiteSpace(firstRow.UnitName))
            {
                if (!unitOfMeasureIdMap.TryGetValue(firstRow.UnitName, out var mappedUnitOfMeasureId))
                {
                    importErrors.Add($"Mã thiết bị '{firstRow.EquipmentCodeDisplay}' có đơn vị tính '{dto.UnitOfMeasureName}' không tồn tại ở dòng {rowNumber}.");
                    continue;
                }

                unitOfMeasureId = mappedUnitOfMeasureId;
            }

            var mappedProcessGroupIds = firstRow.ProcessGroupCodes
                .Select(code => processGroupMap.GetValueOrDefault(code)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (!mappedProcessGroupIds.Any())
            {
                importErrors.Add($"Mã thiết bị '{firstRow.EquipmentCodeDisplay}' không có mã nhóm công đoạn sản xuất hợp lệ ở dòng {rowNumber}.");
                continue;
            }

            // Always create a fresh Id for the snapshot entity to avoid EF tracking conflicts.
            // The update path is resolved by EquipmentCode (dbEquipmentDict), not by this Id.
            var equipmentId = Guid.NewGuid();
            try
            {
                var equipmentEntity = Equipment.Create(equipmentId, dto.Code, dto.Name, unitOfMeasureId);

                // Costs are attached to the snapshot entity for new (add) path only.
                // For the update path, costs are rebuilt separately using the real DB entity Id.
                var costList = costService.ParseExcelCostString(dto.Cost, CostType.Electricity, equipmentId);
                equipmentEntity.AddCost(costList);

                excelDtos.Add((equipmentEntity, rowNumber, firstRow.EquipmentCodeNormalized, mappedProcessGroupIds, dto.Cost ?? string.Empty));
            }
            catch (Exception ex)
            {
                importErrors.Add($"Mã thiết bị '{firstRow.EquipmentCodeDisplay}' có đơn giá '{dto.Cost}' không hợp lệ ở dòng {rowNumber}. Chi tiết: {ex.Message}");
            }
        }

        var dbEquipments = await _equipmentRepository.GetAllAsync(
            include: p => p.Include(p => p.Code).Include(p => p.Costs).Include(p => p.EquipmentProcessGroups),
            disableTracking: false);

        var deleteList = new List<Equipment>();
        var deleteCost = new List<Cost>();
        var insertCostsForUpdate = new List<Cost>(); // Costs rebuilt for updated entities — inserted via repository, not via navigation collection
        var deleteEquipmentProcessGroups = new List<EquipmentProcessGroup>();
        var insertEquipmentProcessGroups = new List<EquipmentProcessGroup>();
        var updateList = new List<Equipment>();
        var addList = new List<Equipment>();

        var excelEquipmentCodes = excelDtos.Select(x => x.EquipmentCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        deleteList.AddRange(dbEquipments.Where(x => !excelEquipmentCodes.Contains(NormalizeEquipmentCode(x.Code?.Value))));
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

        var dbEquipmentDict = dbEquipments
            .Where(p => p.Code != null && !string.IsNullOrWhiteSpace(p.Code.Value))
            .GroupBy(p => NormalizeEquipmentCode(p.Code!.Value), StringComparer.OrdinalIgnoreCase)
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
            var processGroupIdsByRow = excelDto.ProcessGroupIds
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            if (dbEquipmentDict.TryGetValue(excelDto.EquipmentCode, out var entityToUpdate))
            {
                var isInfoChanged = entityToUpdate.CheckChange(dto);
                var isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), dto.Costs.ToList());
                var existingProcessGroupIds = entityToUpdate.EquipmentProcessGroups
                    .Select(epg => epg.ProcessGroupId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();
                var isProcessGroupChanged = !existingProcessGroupIds.SequenceEqual(processGroupIdsByRow);

                if (isInfoChanged || isCostChanged || isProcessGroupChanged)
                {
                    entityToUpdate.Update(dto.Code.Value, dto.Name, dto.UnitOfMeasureId);

                    if (isCostChanged)
                    {
                        // Delete old costs tracked by EF
                        deleteCost.AddRange(entityToUpdate.Costs.ToList());

                        // Do NOT call entityToUpdate.ClearCost() or entityToUpdate.AddCost() here.
                        // Mutating the EF-tracked navigation collection while also deleting via repository
                        // causes EF to insert Cost rows before the delete executes, violating CK_Cost_OneParentOnly.
                        // Instead, rebuild costs with the correct DB entity Id and insert via repository directly.
                        var rebuiltCosts = costService.ParseExcelCostString(excelDto.CostString, CostType.Electricity, entityToUpdate.Id);
                        insertCostsForUpdate.AddRange(rebuiltCosts);
                    }

                    if (isProcessGroupChanged)
                    {
                        deleteEquipmentProcessGroups.AddRange(entityToUpdate.EquipmentProcessGroups.ToList());
                        insertEquipmentProcessGroups.AddRange(processGroupIdsByRow
                            .Select(processGroupId => EquipmentProcessGroup.Create(entityToUpdate.Id, processGroupId)));
                    }

                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsCodeExisted(dto.Code.Value))
                {
                    importErrors.Add($"Mã thiết bị '{dto.Code.Value}' đã tồn tại ở dòng {rowNumber}.");
                    continue;
                }

                addList.Add(dto);
                insertEquipmentProcessGroups.AddRange(processGroupIdsByRow
                    .Select(processGroupId => EquipmentProcessGroup.Create(dto.Id, processGroupId)));
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

    private static bool TryBuildImportRow(EquipmentExcelDto dto, int rowNumber, ICollection<EquipmentImportRow> rows, ICollection<string> errors)
    {
        var equipmentCodeDisplay = (dto.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(equipmentCodeDisplay))
        {
            errors.Add($"Thiếu mã thiết bị ở dòng {rowNumber}.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add($"Mã thiết bị '{equipmentCodeDisplay}' thiếu tên thiết bị ở dòng {rowNumber}.");
            return false;
        }

        var processGroupCodes = ParseProcessGroupCodes(dto.ProcessGroupCodes);
        if (!processGroupCodes.Any())
        {
            errors.Add($"Mã thiết bị '{equipmentCodeDisplay}' thiếu mã nhóm công đoạn sản xuất ở dòng {rowNumber}.");
            return false;
        }

        rows.Add(new EquipmentImportRow(
            dto,
            rowNumber,
            NormalizeEquipmentCode(equipmentCodeDisplay),
            equipmentCodeDisplay,
            dto.Name.Trim(),
            dto.UnitOfMeasureName?.Trim() ?? string.Empty,
            processGroupCodes,
            NormalizeProcessGroupCodes(processGroupCodes),
            NormalizeCostString(dto.Cost)));

        return true;
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

    private static void ValidateConsistentEquipmentRows(IGrouping<string, EquipmentImportRow> rowGroup, ICollection<string> errors)
    {
        var rows = rowGroup.OrderBy(x => x.RowNumber).ToList();
        if (rows.Count <= 1)
        {
            return;
        }

        var lineList = string.Join(", ", rows.Select(r => r.RowNumber));
        errors.Add($"Mã thiết bị '{rows[0].EquipmentCodeDisplay}' bị trùng ở các dòng {lineList}.");
    }

    private static string NormalizeEquipmentCode(string? equipmentCode) => (equipmentCode ?? string.Empty).Trim().ToUpperInvariant();
    private static string NormalizeCostString(string? cost) => (cost ?? string.Empty).Replace(" ", string.Empty).Trim();

    private sealed record EquipmentImportRow(
        EquipmentExcelDto Dto,
        int RowNumber,
        string EquipmentCodeNormalized,
        string EquipmentCodeDisplay,
        string EquipmentName,
        string UnitName,
        List<string> ProcessGroupCodes,
        string ProcessGroupCodesNormalized,
        string CostNormalized);
}