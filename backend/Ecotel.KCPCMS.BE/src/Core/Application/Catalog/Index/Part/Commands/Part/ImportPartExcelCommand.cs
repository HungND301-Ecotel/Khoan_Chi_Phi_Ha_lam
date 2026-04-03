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
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(ImportPartExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<PartExcelDto>(stream);

        var importRows = dtos
            .Select((dto, index) => BuildImportRow(dto, index + 2))
            .ToList();

        var groupedRows = importRows
            .GroupBy(r => r.PartCodeNormalized)
            .ToList();

        foreach (var rowGroup in groupedRows)
        {
            ValidateConsistentPartRows(rowGroup);
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

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => unitNames.Contains(p.Name),
            disableTracking: false);
        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var equipmentEntities = await _equipmentRepository.GetAllAsync(
            predicate: p => equipmentCodes.Contains(p.Code!.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: false);
        var equipmentMap = equipmentEntities.ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
        var equipmentCodeSet = equipmentMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dbUnitOfMeasureNames = unitOfMeasureIdMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in importRows)
        {
            if (string.IsNullOrWhiteSpace(row.UnitName) || !dbUnitOfMeasureNames.Contains(row.UnitName))
            {
                throw new BadRequestException($"Mã phụ tùng '{row.PartCodeDisplay}' có đơn vị tính '{row.Dto.UnitOfMeasureName}' không tồn tại ở dòng {row.RowNumber}.");
            }

            if (!equipmentCodeSet.Contains(row.EquipmentCode))
            {
                throw new BadRequestException($"Mã phụ tùng '{row.PartCodeDisplay}' có mã thiết bị '{row.EquipmentCode}' không tồn tại ở dòng {row.RowNumber}.");
            }
        }

        var excelDtos = new List<(PartEntity Entity, int RowNumber, string PartCode, List<Cost> Costs)>();
        foreach (var rowGroup in groupedRows)
        {
            var firstRow = rowGroup.First();
            var rowNumber = firstRow.RowNumber;
            var dto = firstRow.Dto;

            if (!unitOfMeasureIdMap.TryGetValue(firstRow.UnitName, out var unitOfMeasureId))
            {
                throw new BadRequestException($"Mã phụ tùng '{firstRow.PartCodeDisplay}' có đơn vị tính '{dto.UnitOfMeasureName}' không tồn tại ở dòng {rowNumber}.");
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

            if (!mappedEquipments.Any())
            {
                throw new BadRequestException($"Mã phụ tùng '{firstRow.PartCodeDisplay}' không có mã thiết bị hợp lệ ở dòng {rowNumber}.");
            }

            var partId = Guid.NewGuid();
            var partEntity = PartEntity.Create(partId, dto.Code, dto.Name, unitOfMeasureId, dto.ReplacementTimeStandard, mappedEquipments, PartType.Part);
            try
            {
                var costList = costService.ParseExcelCostString(dto.Cost, CostType.Part, partId);
                costList = RebuildPartCosts(costList, partId);
                ValidateSinglePartCosts(partEntity.Code?.Value ?? firstRow.PartCodeDisplay, costList, partId);
                excelDtos.Add((partEntity, rowNumber, firstRow.PartCodeNormalized, costList));
            }
            catch (Exception ex)
            {
                throw new BadRequestException($"Mã phụ tùng '{firstRow.PartCodeDisplay}' có đơn giá '{dto.Cost}' không hợp lệ ở dòng {rowNumber}. Chi tiết: {ex.Message}");
            }
        }

        var dbParts = await _partRepository.GetAllAsync(
            predicate: p => p.Type == PartType.Part,
            include: p => p
                .Include(p => p.Code)
                .Include(p => p.Costs)
                .Include(p => p.EquipmentParts).ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code),
            disableTracking: false);

        var deleteList = new List<PartEntity>();
        var deleteCost = new List<Cost>();
        var insertCost = new List<Cost>();
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

                throw new ConflictException(
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

            if (dbPartDict.TryGetValue(partCode, out var entityToUpdate))
            {
                bool isInfoChanged = entityToUpdate.CheckChange(dto);
                bool isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), parsedCosts);

                if (isInfoChanged || isCostChanged)
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

                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsPartCodeExisted(dto.Code!.Value))
                {
                    throw new ConflictException($"Mã phụ tùng '{dto.Code!.Value}' đã tồn tại ở dòng {rowNumber}.");
                }

                addList.Add(dto);
                insertCost.AddRange(RebuildPartCosts(parsedCosts, dto.Id));
            }
        }

        ValidatePartCosts(insertCost);

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

                _partRepository.Update(updateList);
            }

            if (insertCost.Any())
            {
                await _costRepository.InsertAsync(insertCost);
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

    private static PartImportRow BuildImportRow(PartExcelDto dto, int rowNumber)
    {
        var partCodeDisplay = (dto.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(partCodeDisplay))
        {
            throw new BadRequestException($"Thiếu mã phụ tùng ở dòng {rowNumber}.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new BadRequestException($"Mã phụ tùng '{partCodeDisplay}' thiếu tên phụ tùng ở dòng {rowNumber}.");
        }

        return new PartImportRow(
            dto,
            rowNumber,
            NormalizePartCode(partCodeDisplay),
            partCodeDisplay,
            dto.Name.Trim(),
            dto.UnitOfMeasureName?.Trim() ?? string.Empty,
            ParseSingleEquipmentCode(dto.EquipmentCode, rowNumber, partCodeDisplay),
            NormalizeCostString(dto.Cost));
    }

    private static string ParseSingleEquipmentCode(string? equipmentCode, int rowNumber, string partCode)
    {
        var codes = (equipmentCode ?? string.Empty)
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToList();

        if (codes.Count != 1)
        {
            throw new BadRequestException($"Mã phụ tùng '{partCode}' có mã thiết bị '{equipmentCode}' không hợp lệ ở dòng {rowNumber}. Mỗi dòng chỉ được khai báo 1 mã thiết bị.");
        }

        return codes[0];
    }

    private static void ValidateConsistentPartRows(IGrouping<string, PartImportRow> rowGroup)
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
            throw new BadRequestException($"Mã phụ tùng '{rows[0].PartCodeDisplay}' bị trùng cặp Mã phụ tùng + Mã thiết bị: {duplicateDetails}.");
        }

        var first = rows[0];
        foreach (var row in rows.Skip(1))
        {
            if (!string.Equals(first.PartName, row.PartName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(first.UnitName, row.UnitName, StringComparison.OrdinalIgnoreCase)
                || first.Dto.ReplacementTimeStandard != row.Dto.ReplacementTimeStandard
                || !string.Equals(first.CostNormalized, row.CostNormalized, StringComparison.OrdinalIgnoreCase))
            {
                var lineList = string.Join(", ", rows.Select(r => r.RowNumber));
                throw new BadRequestException(
                    $"Mã phụ tùng '{first.PartCodeDisplay}' có dữ liệu không đồng nhất ở các dòng {lineList}. " +
                    "Các thông tin phải giống nhau: Tên phụ tùng, Đơn vị tính, Định mức thời gian thay thế, Đơn giá. " +
                    "Chỉ được phép khác Mã thiết bị/Tên thiết bị.");
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

    private static void ValidateSinglePartCosts(string partCode, IEnumerable<Cost> costs, Guid partId)
    {
        foreach (var cost in costs)
        {
            var parentCount =
                (cost.PartId.HasValue ? 1 : 0) +
                (cost.MaterialId.HasValue ? 1 : 0) +
                (cost.EquipmentId.HasValue ? 1 : 0);

            if (parentCount != 1 || cost.PartId != partId)
            {
                throw new BadRequestException($"Mã phụ tùng '{partCode}' có dữ liệu đơn giá không hợp lệ.");
            }
        }
    }

    private static void ValidatePartCosts(IEnumerable<Cost> costs)
    {
        foreach (var cost in costs)
        {
            var parentCount =
                (cost.PartId.HasValue ? 1 : 0) +
                (cost.MaterialId.HasValue ? 1 : 0) +
                (cost.EquipmentId.HasValue ? 1 : 0);

            if (parentCount != 1 || !cost.PartId.HasValue)
            {
                throw new BadRequestException("Dữ liệu đơn giá phụ tùng không hợp lệ.");
            }
        }
    }

    private sealed record PartImportRow(
        PartExcelDto Dto,
        int RowNumber,
        string PartCodeNormalized,
        string PartCodeDisplay,
        string PartName,
        string UnitName,
        string EquipmentCode,
        string CostNormalized);
}
