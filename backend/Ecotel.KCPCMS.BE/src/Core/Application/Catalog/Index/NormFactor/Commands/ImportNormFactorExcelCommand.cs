using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.NormFactor;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using NormFactorAssignmentCodeEntity = Domain.Entities.Index.NormFactorAssignmentCode;
using NormFactorEntity = Domain.Entities.Index.NormFactor;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record ImportNormFactorExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportNormFactorExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ImportNormFactorExcelCommand, bool>
{
    private readonly IWriteRepository<NormFactorEntity> _normFactorRepository =
        unitOfWork.GetRepository<NormFactorEntity>();
    private readonly IWriteRepository<NormFactorAssignmentCodeEntity> _normFactorAssignmentCodeRepository =
        unitOfWork.GetRepository<NormFactorAssignmentCodeEntity>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Domain.Entities.Index.Hardness> _hardnessRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Hardness>();
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    private readonly IWriteRepository<Domain.Entities.Index.AssignmentCode> _assignmentCodeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.AssignmentCode>();

    public async Task<bool> Handle(ImportNormFactorExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<NormFactorExcelDto>(stream);

        var productionProcesses = await _productionProcessRepository.GetAllAsync(
            include: q => q.Include(p => p.Code),
            disableTracking: true);

        var hardnesses = await _hardnessRepository.GetAllAsync(disableTracking: true);
        var stoneClampRatios = await _stoneClampRatioRepository.GetAllAsync(disableTracking: true);
        var assignmentCodes = await _assignmentCodeRepository.GetAllAsync(
            include: q => q.Include(a => a.Code),
            disableTracking: true);

        var productionProcessByCode = productionProcesses
            .Where(p => p.Code != null && !string.IsNullOrWhiteSpace(p.Code!.Value))
            .GroupBy(p => p.Code!.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var productionProcessByDisplay = productionProcesses
            .Where(p => p.Code != null)
            .GroupBy(p => $"{p.Code!.Value} - {p.Name}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var hardnessByName = hardnesses
            .Where(h => !string.IsNullOrWhiteSpace(h.Value))
            .GroupBy(h => h.Value.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var stoneClampByName = stoneClampRatios
            .Where(s => !string.IsNullOrWhiteSpace(s.Value))
            .GroupBy(s => s.Value.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var assignmentCodeByCode = assignmentCodes
            .Where(a => a.Code != null && !string.IsNullOrWhiteSpace(a.Code!.Value))
            .GroupBy(a => a.Code!.Value.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var mappedRows = new List<MappedNormFactorRow>();
        foreach (var dto in dtos)
        {
            var productionProcess = ResolveProductionProcess(dto.ProductionProcessName, productionProcessByCode, productionProcessByDisplay)
                ?? throw new BadRequestException(CustomResponseMessage.ProductionProcessNotFound);

            var hardnessId = ResolveHardnessId(dto.HardnessName, hardnessByName);
            var steelMeshType = ResolveSteelMeshType(dto.SteelMeshTypeName);

            var stoneClampRatioName = dto.StoneClampRatioName?.Trim() ?? string.Empty;
            if (!stoneClampByName.TryGetValue(stoneClampRatioName, out var stoneClampRatio))
            {
                throw new BadRequestException(CustomResponseMessage.StoneClampRatioNotFound);
            }

            var assignmentCodeIds = ResolveAssignmentCodeIds(dto.AffectAssignmentCodes, assignmentCodeByCode);

            var targetHardnessId = ResolveTargetHardnessId(dto.TargetHardnessName, hardnessByName);

            mappedRows.Add(new MappedNormFactorRow(
                dto.Id,
                productionProcess.Id,
                hardnessId,
                stoneClampRatio.Id,
                steelMeshType,
                assignmentCodeIds,
                dto.Value,
                targetHardnessId));
        }

        var dbNormFactors = await _normFactorRepository.GetAllAsync(
            include: q => q.Include(n => n.NormFactorAssignmentCodes),
            disableTracking: false);

        var dbNormFactorDict = dbNormFactors.ToDictionary(n => n.Id);

        var deleteList = new List<NormFactorEntity>();
        var updateList = new List<NormFactorEntity>();
        var addList = new List<NormFactorEntity>();
        var assignmentToDelete = new List<NormFactorAssignmentCodeEntity>();

        var excelIds = mappedRows.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbNormFactors.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var row in mappedRows)
        {
            if (row.Id != Guid.Empty && dbNormFactorDict.TryGetValue(row.Id, out var existingEntity))
            {
                var existingAssignmentIds = existingEntity.NormFactorAssignmentCodes
                    .Select(x => x.AssignmentCodeId)
                    .ToHashSet();

                var newAssignmentIds = row.AssignmentCodeIds.ToHashSet();

                var isChanged =
                    existingEntity.ProductionProcessId != row.ProductionProcessId
                    || existingEntity.HardnessId != row.HardnessId
                    || existingEntity.StoneClampRatioId != row.StoneClampRatioId
                    || existingEntity.SteelMeshType != row.SteelMeshType
                    || existingEntity.TargetHardnessId != row.TargetHardnessId
                    || existingEntity.Value != row.Value
                    || !existingAssignmentIds.SetEquals(newAssignmentIds);

                if (!isChanged)
                {
                    continue;
                }

                existingEntity.Update(
                    row.ProductionProcessId,
                    row.HardnessId,
                    row.StoneClampRatioId,
                    row.Value,
                    row.TargetHardnessId,
                    row.SteelMeshType);

                SyncAssignmentCodes(existingEntity, row.AssignmentCodeIds, assignmentToDelete);
                updateList.Add(existingEntity);
            }
            else
            {
                var newEntity = NormFactorEntity.Create(
                    row.ProductionProcessId,
                    row.HardnessId,
                    row.StoneClampRatioId,
                    row.Value,
                    row.TargetHardnessId,
                    row.SteelMeshType);

                newEntity.AddNormFactorAssignmentCode(
                    row.AssignmentCodeIds
                        .Select(id => NormFactorAssignmentCodeEntity.Create(id, Guid.Empty))
                        .ToList());

                addList.Add(newEntity);
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _normFactorRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _normFactorRepository.InsertAsync(addList, cancellationToken);
            }

            if (assignmentToDelete.Any())
            {
                _normFactorAssignmentCodeRepository.Delete(assignmentToDelete);
            }

            if (updateList.Any())
            {
                _normFactorRepository.Update(updateList);
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

    private static Domain.Entities.Index.ProductionProcess? ResolveProductionProcess(
        string? rawValue,
        IReadOnlyDictionary<string, Domain.Entities.Index.ProductionProcess> productionProcessByCode,
        IReadOnlyDictionary<string, Domain.Entities.Index.ProductionProcess> productionProcessByDisplay)
    {
        var value = rawValue?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (productionProcessByDisplay.TryGetValue(value, out var byDisplay))
        {
            return byDisplay;
        }

        var code = ExtractCode(value);
        if (!string.IsNullOrWhiteSpace(code) && productionProcessByCode.TryGetValue(code, out var byCode))
        {
            return byCode;
        }

        return null;
    }

    private static Guid? ResolveHardnessId(
        string? rawValue,
        IReadOnlyDictionary<string, Domain.Entities.Index.Hardness> hardnessByName)
    {
        var value = rawValue?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value)
            || value.Equals("Không có", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Không áp dụng", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (hardnessByName.TryGetValue(value, out var hardness))
        {
            return hardness.Id;
        }

        throw new BadRequestException(CustomResponseMessage.HardnessNotFound);
    }

    private static Guid? ResolveTargetHardnessId(
        string? rawValue,
        IReadOnlyDictionary<string, Domain.Entities.Index.Hardness> hardnessByName)
    {
        var value = rawValue?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value)
            || value.Equals("Định mức hiện tại", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Không có", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Không áp dụng", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (hardnessByName.TryGetValue(value, out var hardness))
        {
            return hardness.Id;
        }

        throw new BadRequestException(CustomResponseMessage.HardnessNotFound);
    }

    private static SteelMeshType ResolveSteelMeshType(string? rawValue)
    {
        var value = rawValue?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return SteelMeshType.None;
        }

        if (int.TryParse(value, out var enumInt)
            && Enum.IsDefined(typeof(SteelMeshType), enumInt))
        {
            return (SteelMeshType)enumInt;
        }

        if (value.Equals("Không áp dụng", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Không có", StringComparison.OrdinalIgnoreCase)
            || value.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return SteelMeshType.None;
        }

        if (value.Equals("Trải 1 lớp lưới thép", StringComparison.OrdinalIgnoreCase)
            || value.Equals(nameof(SteelMeshType.SingleLayerSteelMesh), StringComparison.OrdinalIgnoreCase))
        {
            return SteelMeshType.SingleLayerSteelMesh;
        }

        if (value.Equals("Trải 2 lớp lưới thép", StringComparison.OrdinalIgnoreCase)
            || value.Equals(nameof(SteelMeshType.DoubleLayerSteelMesh), StringComparison.OrdinalIgnoreCase))
        {
            return SteelMeshType.DoubleLayerSteelMesh;
        }

        throw new BadRequestException("STEEL_MESH_TYPE_NOT_VALID");
    }

    private static List<Guid> ResolveAssignmentCodeIds(
        string? rawValue,
        IReadOnlyDictionary<string, Domain.Entities.Index.AssignmentCode> assignmentCodeByCode)
    {
        var codes = (rawValue ?? string.Empty)
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var assignmentIds = new List<Guid>();
        foreach (var code in codes)
        {
            var normalizedCode = ExtractCode(code);
            if (string.IsNullOrWhiteSpace(normalizedCode)
                || !assignmentCodeByCode.TryGetValue(normalizedCode, out var assignmentCode))
            {
                throw new BadRequestException(CustomResponseMessage.AssignmentCodeNotFound);
            }

            assignmentIds.Add(assignmentCode.Id);
        }

        return assignmentIds;
    }

    private static string ExtractCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        var separatorIndex = normalized.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return normalized;
        }

        return normalized[..separatorIndex].Trim();
    }

    private static void SyncAssignmentCodes(
        NormFactorEntity normFactor,
        IList<Guid> newAssignmentIds,
        List<NormFactorAssignmentCodeEntity> toDelete)
    {
        var existing = normFactor.NormFactorAssignmentCodes.ToList();
        var existingIds = existing.Select(x => x.AssignmentCodeId).ToHashSet();
        var newIds = newAssignmentIds.ToHashSet();

        var keep = existing.Where(x => newIds.Contains(x.AssignmentCodeId)).ToList();
        var add = newIds
            .Except(existingIds)
            .Select(id => NormFactorAssignmentCodeEntity.Create(id, normFactor.Id))
            .ToList();
        var remove = existing.Where(x => !newIds.Contains(x.AssignmentCodeId)).ToList();

        if (remove.Any())
        {
            toDelete.AddRange(remove);
        }

        keep.AddRange(add);
        normFactor.AddNormFactorAssignmentCode(keep);
    }

    private sealed record MappedNormFactorRow(
        Guid Id,
        Guid ProductionProcessId,
        Guid? HardnessId,
        Guid StoneClampRatioId,
        SteelMeshType SteelMeshType,
        IList<Guid> AssignmentCodeIds,
        double Value,
        Guid? TargetHardnessId);
}
