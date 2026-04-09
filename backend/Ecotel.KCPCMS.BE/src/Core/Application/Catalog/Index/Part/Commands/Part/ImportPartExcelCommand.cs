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
    private readonly IWriteRepository<Code> _codeRepository = unitOfWork.GetRepository<Code>();

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
            TryBuildImportRow(dtos[i], i + 2, importRows, importErrors);
        }

        ValidateDuplicatedPartCode(importRows, importErrors);

        var unitNames = importRows
            .Select(r => r.UnitName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => unitNames.Contains(p.Name),
            disableTracking: false);
        var unitOfMeasureIdMap = unitOfMeasures.ToDictionary(p => p.Name.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase);
        var dbUnitOfMeasureNames = unitOfMeasureIdMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in importRows)
        {
            if (string.IsNullOrWhiteSpace(row.UnitName) || !dbUnitOfMeasureNames.Contains(row.UnitName))
            {
                importErrors.Add($"Mã phụ tùng '{row.PartCodeDisplay}' có đơn vị tính '{row.Dto.UnitOfMeasureName}' không tồn tại ở dòng {row.RowNumber}.");
            }
        }

        var excelDtos = new List<(PartEntity Entity, int RowNumber, string PartCodeNormalized, List<Cost> Costs)>();
        foreach (var row in importRows)
        {
            if (!unitOfMeasureIdMap.TryGetValue(row.UnitName, out var unitOfMeasureId))
            {
                continue;
            }

            var partId = Guid.NewGuid();
            var partEntity = PartEntity.Create(partId, row.Dto.Code, row.Dto.Name, unitOfMeasureId, row.Dto.ReplacementTimeStandard, PartType.Part);

            try
            {
                var costs = costService.ParseExcelCostString(row.Dto.Cost, CostType.Part, partId)
                    .Select(c => Cost.Create(c.StartMonth, c.EndMonth, CostType.Part, c.Amount, partId, c.ActualAmount))
                    .ToList();

                if (!TryValidatePartCosts(costs, out var partCostError))
                {
                    importErrors.Add($"Mã phụ tùng '{row.PartCodeDisplay}' {partCostError}");
                    continue;
                }

                excelDtos.Add((partEntity, row.RowNumber, row.PartCodeNormalized, costs));
            }
            catch (Exception ex)
            {
                importErrors.Add($"Mã phụ tùng '{row.PartCodeDisplay}' có đơn giá '{row.Dto.Cost}' không hợp lệ ở dòng {row.RowNumber}. Chi tiết: {ex.Message}");
            }
        }

        ThrowIfImportErrors(importErrors);

        var dbParts = await _partRepository.GetAllAsync(
            predicate: p => p.Type == PartType.Part,
            include: p => p.Include(p => p.Code).Include(p => p.Costs),
            disableTracking: false);

        var dbPartDict = dbParts
            .Where(p => p.Code != null && !string.IsNullOrWhiteSpace(p.Code.Value))
            .ToDictionary(p => NormalizePartCode(p.Code!.Value), p => p, StringComparer.OrdinalIgnoreCase);

        var excelPartCodes = excelDtos.Select(x => x.PartCodeNormalized).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var deleteList = dbParts.Where(x => x.Code != null && !excelPartCodes.Contains(NormalizePartCode(x.Code.Value))).ToList();
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

        var deleteCosts = new List<Cost>();
        var insertCosts = new List<Cost>();
        var updateList = new List<PartEntity>();
        var addList = new List<PartEntity>();

        foreach (var excelDto in excelDtos)
        {
            var dto = excelDto.Entity;
            var partCodeNormalized = excelDto.PartCodeNormalized;
            var parsedCosts = excelDto.Costs;

            if (dbPartDict.TryGetValue(partCodeNormalized, out var entityToUpdate))
            {
                var isInfoChanged = !string.Equals(entityToUpdate.Name, dto.Name, StringComparison.Ordinal)
                    || entityToUpdate.UnitOfMeasureId != dto.UnitOfMeasureId
                    || entityToUpdate.ReplacementTimeStandard != dto.ReplacementTimeStandard
                    || entityToUpdate.Type != PartType.Part;

                var isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), parsedCosts);

                if (isInfoChanged || isCostChanged)
                {
                    entityToUpdate.Update(
                        dto.Code!.Value,
                        dto.Name,
                        dto.UnitOfMeasureId,
                        dto.ReplacementTimeStandard,
                        PartType.Part);

                    if (isCostChanged)
                    {
                        deleteCosts.AddRange(entityToUpdate.Costs.ToList());
                        insertCosts.AddRange(parsedCosts.Select(c => Cost.Create(c.StartMonth, c.EndMonth, CostType.Part, c.Amount, entityToUpdate.Id, c.ActualAmount)));
                    }

                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsPartCodeExisted(dto.Code!.Value))
                {
                    throw new ConflictException($"Mã phụ tùng '{dto.Code!.Value}' đã tồn tại ở dòng {excelDto.RowNumber}.");
                }

                addList.Add(dto);
                insertCosts.AddRange(parsedCosts);
            }
        }

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
                if (deleteCosts.Any())
                {
                    _costRepository.Delete(deleteCosts);
                }

                _partRepository.Update(updateList);
            }

            if (insertCosts.Any())
            {
                await _costRepository.InsertAsync(insertCosts);
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

        rows.Add(new PartImportRow(
            dto,
            rowNumber,
            NormalizePartCode(partCodeDisplay),
            partCodeDisplay,
            dto.UnitOfMeasureName?.Trim() ?? string.Empty));

        return true;
    }

    private static void ValidateDuplicatedPartCode(IEnumerable<PartImportRow> rows, ICollection<string> errors)
    {
        foreach (var group in rows.GroupBy(x => x.PartCodeNormalized))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            var lines = string.Join(", ", group.Select(x => x.RowNumber));
            errors.Add($"Mã phụ tùng '{group.First().PartCodeDisplay}' bị trùng ở các dòng {lines}.");
        }
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
                error = "có dữ liệu đơn giá không hợp lệ.";
                return false;
            }
        }

        error = string.Empty;
        return true;
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

    private static string NormalizePartCode(string? partCode) => (partCode ?? string.Empty).Trim().ToUpperInvariant();

    private sealed record PartImportRow(
        PartExcelDto Dto,
        int RowNumber,
        string PartCodeNormalized,
        string PartCodeDisplay,
        string UnitName);
}
