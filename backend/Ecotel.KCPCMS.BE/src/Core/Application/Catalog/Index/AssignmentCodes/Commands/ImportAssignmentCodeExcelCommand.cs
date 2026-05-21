using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AssignmentCodeEntity = Domain.Entities.Index.AssignmentCode;

namespace Application.Catalog.Index.AssignmentCodes.Commands;

public record ImportAssignmentCodeExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportAssignmentCodeExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportAssignmentCodeExcelCommand, bool>
{
    private readonly IWriteRepository<AssignmentCodeEntity> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCodeEntity>();
    private readonly IWriteRepository<AssignmentCodeMaterial> _assignmentCodeMaterialRepository = unitOfWork.GetRepository<AssignmentCodeMaterial>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    private readonly IWriteRepository<Code> _codeRepository = unitOfWork.GetRepository<Code>();

    public async Task<bool> Handle(ImportAssignmentCodeExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<AssignmentCodeExcelDto>(stream) ?? [];

        var importErrors = new List<string>();
        var normalizedRows = BuildNormalizedRows(dtos, importErrors);

        var groupedRows = normalizedRows
            .GroupBy(r => r.AssignmentCodeNormalized, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitNames = normalizedRows
            .Select(r => r.UnitName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var materialCodes = normalizedRows
            .Select(r => r.MaterialCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(
            predicate: u => unitNames.Contains(u.Name),
            disableTracking: true);
        var unitIdMap = unitOfMeasures.ToDictionary(u => u.Name.Trim(), u => u.Id, StringComparer.OrdinalIgnoreCase);

        var materials = await _materialRepository.GetAllAsync(
            predicate: m => m.Code != null && materialCodes.Contains(m.Code.Value),
            include: m => m.Include(x => x.Code),
            disableTracking: false);
        var materialByCodeMap = materials
            .Where(m => m.Code != null && !string.IsNullOrWhiteSpace(m.Code.Value))
            .GroupBy(m => NormalizeCode(m.Code!.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var row in normalizedRows)
        {
            if (!string.IsNullOrWhiteSpace(row.UnitName) && !unitIdMap.ContainsKey(row.UnitName))
            {
                importErrors.Add($"Nhóm vật tư, tài sản '{row.AssignmentCodeDisplay}' có đơn vị tính '{row.UnitName}' không tồn tại ở dòng {row.RowNumber}.");
            }

            if (!string.IsNullOrWhiteSpace(row.MaterialCode) && !materialByCodeMap.ContainsKey(row.MaterialCode))
            {
                importErrors.Add($"Nhóm vật tư, tài sản '{row.AssignmentCodeDisplay}' có mã vật tư, tài sản '{row.MaterialCode}' không tồn tại ở dòng {row.RowNumber}.");
            }
        }

        ThrowIfImportErrors(importErrors);

        var importItems = groupedRows.Select(group =>
        {
            var rows = group.OrderBy(r => r.RowNumber).ToList();
            var firstRow = rows.First();

            var mappedMaterialIds = rows
                .Select(r => r.MaterialCode)
                .Where(code => !string.IsNullOrWhiteSpace(code) && materialByCodeMap.ContainsKey(code))
                .Select(code => materialByCodeMap[code].Id)
                .Distinct()
                .ToList();

            Guid? unitOfMeasureId = null;
            if (!string.IsNullOrWhiteSpace(firstRow.UnitName)
                && unitIdMap.TryGetValue(firstRow.UnitName, out var mappedUnitId))
            {
                unitOfMeasureId = mappedUnitId;
            }

            return new AssignmentCodeImportItem(
                RowNumber: firstRow.RowNumber,
                AssignmentCodeDisplay: firstRow.AssignmentCodeDisplay,
                AssignmentCodeNormalized: firstRow.AssignmentCodeNormalized,
                Name: firstRow.AssignmentName,
                UnitOfMeasureId: unitOfMeasureId,
                MaterialIds: mappedMaterialIds);
        }).ToList();

        var dbAssignmentCodes = await _assignmentCodeRepository.GetAllAsync(
            include: a => a
                .Include(a => a.Code!)
                .Include(a => a.AssignmentCodeMaterials).ThenInclude(m => m.Material).ThenInclude(m => m.Code),
            disableTracking: false);

        var dbByCode = dbAssignmentCodes
            .Where(a => a.Code != null && !string.IsNullOrWhiteSpace(a.Code.Value))
            .GroupBy(a => NormalizeCode(a.Code!.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var excelCodeSet = importItems
            .Select(i => i.AssignmentCodeNormalized)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var deleteList = dbAssignmentCodes
            .Where(a => a.Code == null || !excelCodeSet.Contains(NormalizeCode(a.Code.Value)))
            .ToList();
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

        var addList = new List<AssignmentCodeEntity>();
        var updateList = new List<AssignmentCodeEntity>();
        var newAssignmentMaterialMap = new Dictionary<AssignmentCodeEntity, List<Guid>>();

        foreach (var item in importItems)
        {
            if (dbByCode.TryGetValue(item.AssignmentCodeNormalized, out var existedEntity))
            {
                var isInfoChanged =
                    !string.Equals(existedEntity.Name, item.Name, StringComparison.Ordinal)
                    || existedEntity.UnitOfMeasureId != item.UnitOfMeasureId
                    || !string.Equals(existedEntity.Code?.Value, item.AssignmentCodeDisplay, StringComparison.OrdinalIgnoreCase);

                var selectedMaterialIdSet = item.MaterialIds.ToHashSet();
                var existingMaterialIdSet = existedEntity.AssignmentCodeMaterials.Select(m => m.MaterialId).ToHashSet();
                var isMaterialChanged = !selectedMaterialIdSet.SetEquals(existingMaterialIdSet);

                if (isInfoChanged)
                {
                    existedEntity.Update(item.Name, item.AssignmentCodeDisplay, item.UnitOfMeasureId);
                }

                if (isMaterialChanged)
                {
                    var linksToDelete = existedEntity.AssignmentCodeMaterials
                        .Where(m => !selectedMaterialIdSet.Contains(m.MaterialId))
                        .ToList();
                    if (linksToDelete.Any())
                    {
                        _assignmentCodeMaterialRepository.Delete(linksToDelete);
                    }

                    var existingLinkedMaterialIds = existedEntity.AssignmentCodeMaterials
                        .Select(link => link.MaterialId)
                        .ToHashSet();

                    foreach (var materialId in selectedMaterialIdSet.Where(id => !existingLinkedMaterialIds.Contains(id)))
                    {
                        await _assignmentCodeMaterialRepository.InsertAsync(
                            AssignmentCodeMaterial.Create(existedEntity.Id, materialId),
                            cancellationToken);
                    }

                    foreach (var material in materials.Where(m => selectedMaterialIdSet.Contains(m.Id) && m.AssigmentCodeId == null))
                    {
                        material.Update(
                            material.Code?.Value ?? string.Empty,
                            material.Name,
                            material.UnitOfMeasureId,
                            existedEntity.Id,
                            material.MaterialType);
                    }
                }

                if (isInfoChanged || isMaterialChanged)
                {
                    updateList.Add(existedEntity);
                }
            }
            else
            {
                var newEntity = AssignmentCodeEntity.Create(item.Name, item.AssignmentCodeDisplay, item.UnitOfMeasureId);
                addList.Add(newEntity);
                newAssignmentMaterialMap[newEntity] = item.MaterialIds;
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _assignmentCodeRepository.Delete(deleteList);
                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
            }

            if (addList.Any())
            {
                await _assignmentCodeRepository.InsertAsync(addList, cancellationToken);
                await unitOfWork.SaveChangesAsync();

                foreach (var pair in newAssignmentMaterialMap)
                {
                    var assignmentCode = pair.Key;
                    var materialIds = pair.Value.Distinct().ToList();

                    if (materialIds.Any())
                    {
                        await _assignmentCodeMaterialRepository.InsertAsync(
                            materialIds.Select(materialId => AssignmentCodeMaterial.Create(assignmentCode.Id, materialId)).ToList(),
                            cancellationToken);
                    }

                    foreach (var material in materials.Where(m => materialIds.Contains(m.Id) && m.AssigmentCodeId == null))
                    {
                        material.Update(
                            material.Code?.Value ?? string.Empty,
                            material.Name,
                            material.UnitOfMeasureId,
                            assignmentCode.Id,
                            material.MaterialType);
                    }
                }
            }

            if (updateList.Any())
            {
                _assignmentCodeRepository.Update(updateList);
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

    private static List<AssignmentCodeImportRow> BuildNormalizedRows(IList<AssignmentCodeExcelDto> dtos, ICollection<string> errors)
    {
        var rows = new List<AssignmentCodeImportRow>();

        string? currentCode = null;
        string? currentName = null;
        string currentUnit = string.Empty;

        for (var i = 0; i < dtos.Count; i++)
        {
            var rowNumber = i + 2;
            var dto = dtos[i];

            var codeInput = (dto.Code ?? string.Empty).Trim();
            var nameInput = (dto.Name ?? string.Empty).Trim();
            var unitInput = (dto.UnitOfMeasureName ?? string.Empty).Trim();
            var materialInput = NormalizeCode(dto.MaterialCode);

            if (!string.IsNullOrWhiteSpace(codeInput))
            {
                if (string.IsNullOrWhiteSpace(nameInput))
                {
                    errors.Add($"Nhóm vật tư, tài sản '{codeInput}' thiếu tên nhóm vật tư, tài sản ở dòng {rowNumber}.");
                    continue;
                }

                currentCode = codeInput;
                currentName = nameInput;
                currentUnit = unitInput;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(currentCode))
                {
                    errors.Add($"Thiếu nhóm vật tư, tài sản ở dòng {rowNumber}.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(nameInput))
                {
                    currentName = nameInput;
                }

                if (!string.IsNullOrWhiteSpace(unitInput))
                {
                    currentUnit = unitInput;
                }
            }

            if (string.IsNullOrWhiteSpace(currentName))
            {
                errors.Add($"Nhóm vật tư, tài sản '{currentCode}' thiếu tên nhóm vật tư, tài sản ở dòng {rowNumber}.");
                continue;
            }

            rows.Add(new AssignmentCodeImportRow(
                RowNumber: rowNumber,
                AssignmentCodeDisplay: currentCode!,
                AssignmentCodeNormalized: NormalizeCode(currentCode),
                AssignmentName: currentName,
                UnitName: currentUnit,
                MaterialCode: materialInput));
        }

        return rows;
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

    private static string NormalizeCode(string? value)
    {
        var upper = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(upper))
        {
            return string.Empty;
        }

        return string.Join(' ', upper.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed record AssignmentCodeImportRow(
        int RowNumber,
        string AssignmentCodeDisplay,
        string AssignmentCodeNormalized,
        string AssignmentName,
        string UnitName,
        string MaterialCode);

    private sealed record AssignmentCodeImportItem(
        int RowNumber,
        string AssignmentCodeDisplay,
        string AssignmentCodeNormalized,
        string Name,
        Guid? UnitOfMeasureId,
        List<Guid> MaterialIds);
}
