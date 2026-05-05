using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AdjustmentFactorEntity = Domain.Entities.Index.AdjustmentFactor;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record ImportAdjustmentFactorExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportAdjustmentFactorExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportAdjustmentFactorExcelCommand, bool>
{
    private readonly IWriteRepository<AdjustmentFactorEntity> _adjustmentFactorRepository = unitOfWork.GetRepository<AdjustmentFactorEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Code> _codeRepository = unitOfWork.GetRepository<Code>();
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();
    public async Task<bool> Handle(ImportAdjustmentFactorExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<AdjustmentFactorExcelDto>(stream) ?? [];

        var processGroups = await _processGroupRepository.GetAllAsync(
                include: p => p.Include(p => p.Code),
            disableTracking: true);
        var processGroupIdMap = processGroups
            .Where(p => p.Code != null && !string.IsNullOrWhiteSpace(p.Code.Value))
            .ToDictionary(p => p.Code!.Value.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase);
        var fixedKeys = await _fixedKeyRepository.GetAllAsync(
            predicate: fk => fk.Type >= FixedKeyType.K1 && fk.Type <= FixedKeyType.K8,
            disableTracking: true);
        var fixedKeyMap = fixedKeys
            .Where(fk => !string.IsNullOrWhiteSpace(fk.Key))
            .ToDictionary(fk => fk.Key.Trim(), fk => fk, StringComparer.OrdinalIgnoreCase);

        var errors = new List<string>();
        var excelRows = new List<ImportRow>();
        var duplicateChecker = new Dictionary<(Guid ProcessGroupId, Guid FixedKeyId), int>();

        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var rowNumber = i + 2;
            var processGroupCode = dto.ProcessGroupCode?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(processGroupCode) || !processGroupIdMap.TryGetValue(processGroupCode, out var processGroupId))
            {
                errors.Add($"Giá trị mã nhóm công đoạn '{dto.ProcessGroupCode}' không tồn tại ở dòng {rowNumber}.");
                continue;
            }

            var fixedKeyCode = dto.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fixedKeyCode) || !fixedKeyMap.TryGetValue(fixedKeyCode, out var fixedKey))
            {
                errors.Add($"Giá trị mã hệ số điều chỉnh '{dto.Code}' không tồn tại ở dòng {rowNumber}.");
                continue;
            }

            var duplicateKey = (processGroupId, fixedKey.Id);
            if (duplicateChecker.TryGetValue(duplicateKey, out var firstRow))
            {
                errors.Add($"Giá trị mã '{dto.Code}' bị trùng trong cùng nhóm công đoạn (dòng {firstRow} và dòng {rowNumber}).");
            }
            else
            {
                duplicateChecker[duplicateKey] = rowNumber;
            }

            excelRows.Add(new ImportRow
            {
                Dto = dto,
                ProcessGroupId = processGroupId,
                FixedKey = fixedKey,
                RowNumber = rowNumber
            });
        }


        var dbAdjustmentFactor = await _adjustmentFactorRepository.GetAllAsync(
            include: a => a.Include(a => a.Code),
            disableTracking: true);

        var deleteList = new List<AdjustmentFactorEntity>();
        var updateList = new List<AdjustmentFactorEntity>();
        var addList = new List<AdjustmentFactorEntity>();

        //CheckDelete
        var excelIds = excelRows.Select(x => x.Dto.Id).Where(id => id != Guid.Empty).Distinct().ToList();
        var entitiesToDelete = dbAdjustmentFactor.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();
        var deleteIdSet = deleteList.Select(x => x.Id).ToHashSet();

        for (var i = 0; i < excelRows.Count; i++)
        {
            var row = excelRows[i];
            var dto = row.Dto;
            var rowNumber = row.RowNumber;

            if (dto.Id != Guid.Empty && dbAdjustmentFactor.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbAdjustmentFactor.First(x => x.Id == dto.Id);
                var isChanged = entityToUpdate.ProcessGroupId != row.ProcessGroupId
                                || entityToUpdate.FixedKeyId != row.FixedKey.Id
                                || !string.Equals(entityToUpdate.Name, dto.Name, StringComparison.Ordinal)
                                || !string.Equals(entityToUpdate.Code?.Value, row.FixedKey.Key, StringComparison.OrdinalIgnoreCase);

                if (isChanged)
                {
                    var isCodeDuplicatedInDb = dbAdjustmentFactor.Any(x =>
                        x.Id != entityToUpdate.Id
                        && !deleteIdSet.Contains(x.Id)
                        && x.ProcessGroupId == row.ProcessGroupId
                        && x.FixedKeyId == row.FixedKey.Id);

                    if (isCodeDuplicatedInDb)
                    {
                        errors.Add($"Giá trị mã '{row.FixedKey.Key}' đã tồn tại trong cùng nhóm công đoạn ở dòng {rowNumber}.");
                        continue;
                    }

                    try
                    {
                        entityToUpdate.Update(row.FixedKey, dto.Name, row.ProcessGroupId);
                        updateList.Add(entityToUpdate);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Dòng {rowNumber}: {ex.Message}");
                    }
                }
            }
            else
            {
                var isCodeDuplicatedInDb = dbAdjustmentFactor.Any(x =>
                    !deleteIdSet.Contains(x.Id)
                    && x.ProcessGroupId == row.ProcessGroupId
                    && x.FixedKeyId == row.FixedKey.Id);

                if (isCodeDuplicatedInDb)
                {
                    errors.Add($"Giá trị mã '{row.FixedKey.Key}' đã tồn tại trong cùng nhóm công đoạn ở dòng {rowNumber}.");
                    continue;
                }

                try
                {
                    addList.Add(AdjustmentFactorEntity.Create(row.FixedKey, dto.Name, row.ProcessGroupId));
                }
                catch (Exception ex)
                {
                    errors.Add($"Dòng {rowNumber}: {ex.Message}");
                }
            }
        }

        ThrowIfImportErrors(errors);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _adjustmentFactorRepository.Delete(deleteList);

                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
            }

            if (addList.Any())
            {
                await _adjustmentFactorRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _adjustmentFactorRepository.Update(updateList);
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

    private class ImportRow
    {
        public AdjustmentFactorExcelDto Dto { get; set; } = default!;
        public Guid ProcessGroupId { get; set; }
        public FixedKey FixedKey { get; set; } = default!;
        public int RowNumber { get; set; }
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
}
