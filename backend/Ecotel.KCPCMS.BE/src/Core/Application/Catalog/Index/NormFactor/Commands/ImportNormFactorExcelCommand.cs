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
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Material>();

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
        var materials = await _materialRepository.GetAllAsync(
            include: q => q.Include(m => m.Code),
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

        var materialByCode = materials
            .Where(m => m.Code != null && !string.IsNullOrWhiteSpace(m.Code!.Value))
            .GroupBy(m => m.Code!.Value.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var mappedRows = new List<MappedNormFactorAssignmentRow>();
        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var rowNumber = i + 2;
            var productionProcess = ResolveProductionProcess(dto.ProductionProcessName, productionProcessByCode, productionProcessByDisplay)
                ?? throw new BadRequestException($"Giá trị công đoạn sản xuất '{dto.ProductionProcessName}' không tồn tại ở dòng {rowNumber}.");

            var hardnessId = ResolveHardnessId(dto.HardnessName, hardnessByName, rowNumber);
            var steelMeshType = ResolveSteelMeshType(dto.SteelMeshTypeName, rowNumber);

            var stoneClampRatioName = dto.StoneClampRatioName?.Trim() ?? string.Empty;
            if (!stoneClampByName.TryGetValue(stoneClampRatioName, out var stoneClampRatio))
            {
                throw new BadRequestException($"Giá trị tỷ lệ ngậm đá '{dto.StoneClampRatioName}' không tồn tại ở dòng {rowNumber}.");
            }

            var assignmentCodeId = ResolveAssignmentCodeId(dto.AssignmentCode, assignmentCodeByCode, rowNumber);
            var materialId = ResolveMaterialId(dto.MaterialCode, materialByCode, rowNumber);
            var targetHardnessId = ResolveTargetHardnessId(dto.TargetHardnessName, hardnessByName, rowNumber);

            mappedRows.Add(new MappedNormFactorAssignmentRow(
                dto.Id,
                rowNumber,
                productionProcess.Id,
                hardnessId,
                stoneClampRatio.Id,
                steelMeshType,
                assignmentCodeId,
                materialId,
                dto.Value,
                targetHardnessId));
        }

        var aggregates = BuildAggregates(mappedRows);

        var dbNormFactors = await _normFactorRepository.GetAllAsync(
            include: q => q.Include(n => n.NormFactorAssignmentCodes),
            disableTracking: false);
        var dbNormFactorDict = dbNormFactors.ToDictionary(n => n.Id);

        var deleteList = new List<NormFactorEntity>();
        var updateList = new List<NormFactorEntity>();
        var addList = new List<NormFactorEntity>();
        var assignmentToDelete = new List<NormFactorAssignmentCodeEntity>();

        var excelExistingIds = aggregates.Where(x => x.Id != Guid.Empty).Select(x => x.Id).ToList();
        deleteList.AddRange(dbNormFactors.Where(x => !excelExistingIds.Contains(x.Id)));

        foreach (var aggregate in aggregates)
        {
            if (aggregate.Id != Guid.Empty && dbNormFactorDict.TryGetValue(aggregate.Id, out var existingEntity))
            {
                if (!IsNormFactorChanged(existingEntity, aggregate))
                {
                    continue;
                }

                existingEntity.Update(
                    aggregate.ProductionProcessId,
                    aggregate.HardnessId,
                    aggregate.StoneClampRatioId,
                    aggregate.SteelMeshType);

                if (existingEntity.NormFactorAssignmentCodes.Any())
                {
                    assignmentToDelete.AddRange(existingEntity.NormFactorAssignmentCodes);
                }

                existingEntity.AddNormFactorAssignmentCode(aggregate.AssignmentCodes
                    .Select(x => NormFactorAssignmentCodeEntity.Create(
                        assignmentCodeId: x.AssignmentCodeId,
                        normFactorId: existingEntity.Id,
                        materialId: x.MaterialId,
                        value: x.Value,
                        targetHardnessId: x.TargetHardnessId))
                    .ToList());

                updateList.Add(existingEntity);
            }
            else
            {
                var newEntity = NormFactorEntity.Create(
                    aggregate.ProductionProcessId,
                    aggregate.HardnessId,
                    aggregate.StoneClampRatioId,
                    aggregate.SteelMeshType);

                newEntity.AddNormFactorAssignmentCode(aggregate.AssignmentCodes
                    .Select(x => NormFactorAssignmentCodeEntity.Create(
                        assignmentCodeId: x.AssignmentCodeId,
                        normFactorId: Guid.Empty,
                        materialId: x.MaterialId,
                        value: x.Value,
                        targetHardnessId: x.TargetHardnessId))
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

    private static List<MappedNormFactorAggregate> BuildAggregates(List<MappedNormFactorAssignmentRow> rows)
    {
        var byGroup = new Dictionary<string, MappedNormFactorAggregate>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var key = row.Id != Guid.Empty
                ? $"ID:{row.Id:D}"
                : $"NEW:{row.ProductionProcessId:D}:{row.HardnessId?.ToString("D") ?? "NULL"}:{row.StoneClampRatioId:D}:{(int)row.SteelMeshType}";

            if (!byGroup.TryGetValue(key, out var aggregate))
            {
                aggregate = new MappedNormFactorAggregate(
                    row.Id,
                    row.ProductionProcessId,
                    row.HardnessId,
                    row.StoneClampRatioId,
                    row.SteelMeshType);
                byGroup[key] = aggregate;
            }

            if (aggregate.ProductionProcessId != row.ProductionProcessId
                || aggregate.HardnessId != row.HardnessId
                || aggregate.StoneClampRatioId != row.StoneClampRatioId
                || aggregate.SteelMeshType != row.SteelMeshType)
            {
                throw new BadRequestException($"Thông tin nền của NormFactor không đồng nhất ở dòng {row.RowNumber}.");
            }

            if (aggregate.AssignmentCodes.Any(x => x.MaterialId == row.MaterialId))
            {
                throw new BadRequestException($"Vật tư bị trùng lặp trong cùng một NormFactor ở dòng {row.RowNumber}.");
            }

            aggregate.AssignmentCodes.Add(new NormFactorAssignmentCodeUpsertDto
            {
                AssignmentCodeId = row.AssignmentCodeId,
                MaterialId = row.MaterialId,
                Value = row.Value,
                TargetHardnessId = row.TargetHardnessId
            });
        }

        return byGroup.Values.ToList();
    }

    private static bool IsNormFactorChanged(NormFactorEntity existingEntity, MappedNormFactorAggregate aggregate)
    {
        if (existingEntity.ProductionProcessId != aggregate.ProductionProcessId
            || existingEntity.HardnessId != aggregate.HardnessId
            || existingEntity.StoneClampRatioId != aggregate.StoneClampRatioId
            || existingEntity.SteelMeshType != aggregate.SteelMeshType)
        {
            return true;
        }

        var existingMap = existingEntity.NormFactorAssignmentCodes
            .ToDictionary(x => x.MaterialId, x => (x.AssignmentCodeId, x.Value, x.TargetHardnessId));
        var newMap = aggregate.AssignmentCodes
            .ToDictionary(x => x.MaterialId, x => (x.AssignmentCodeId, x.Value, x.TargetHardnessId));

        if (existingMap.Count != newMap.Count)
        {
            return true;
        }

        foreach (var pair in newMap)
        {
            if (!existingMap.TryGetValue(pair.Key, out var oldValue))
            {
                return true;
            }

            if (oldValue.AssignmentCodeId != pair.Value.AssignmentCodeId ||
                oldValue.Value != pair.Value.Value ||
                oldValue.TargetHardnessId != pair.Value.TargetHardnessId)
            {
                return true;
            }
        }

        return false;
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
        IReadOnlyDictionary<string, Domain.Entities.Index.Hardness> hardnessByName,
        int rowNumber)
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

        throw new BadRequestException($"Giá trị độ cứng '{rawValue}' không tồn tại ở dòng {rowNumber}.");
    }

    private static Guid ResolveAssignmentCodeId(
        string? rawValue,
        IReadOnlyDictionary<string, Domain.Entities.Index.AssignmentCode> assignmentCodeByCode,
        int rowNumber)
    {
        var code = ExtractCode(rawValue ?? string.Empty);
        if (string.IsNullOrWhiteSpace(code) || !assignmentCodeByCode.TryGetValue(code, out var assignmentCode))
        {
            throw new BadRequestException($"Giá trị nhóm vật tư, tài sản '{rawValue}' không tồn tại ở dòng {rowNumber}.");
        }
        return assignmentCode.Id;
    }

    private static Guid ResolveMaterialId(
        string? rawValue,
        IReadOnlyDictionary<string, Domain.Entities.Index.Material> materialByCode,
        int rowNumber)
    {
        var code = ExtractCode(rawValue ?? string.Empty);
        if (string.IsNullOrWhiteSpace(code) || !materialByCode.TryGetValue(code, out var material))
        {
            throw new BadRequestException($"Giá trị mã vật tư '{rawValue}' không tồn tại ở dòng {rowNumber}.");
        }
        return material.Id;
    }

    private static Guid? ResolveTargetHardnessId(
        string? rawValue,
        IReadOnlyDictionary<string, Domain.Entities.Index.Hardness> hardnessByName,
        int rowNumber)
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

        throw new BadRequestException($"Giá trị độ cứng mục tiêu '{rawValue}' không tồn tại ở dòng {rowNumber}.");
    }

    private static SteelMeshType ResolveSteelMeshType(string? rawValue, int rowNumber)
    {
        var value = rawValue?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return SteelMeshType.None;
        }

        if (int.TryParse(value, out var enumInt) && Enum.IsDefined(typeof(SteelMeshType), enumInt))
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

        throw new BadRequestException($"Giá trị loại lưới thép '{rawValue}' không hợp lệ ở dòng {rowNumber}.");
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

    private sealed record MappedNormFactorAssignmentRow(
        Guid Id,
        int RowNumber,
        Guid ProductionProcessId,
        Guid? HardnessId,
        Guid StoneClampRatioId,
        SteelMeshType SteelMeshType,
        Guid AssignmentCodeId,
        Guid MaterialId,
        double Value,
        Guid? TargetHardnessId);

    private sealed class MappedNormFactorAggregate(
        Guid id,
        Guid productionProcessId,
        Guid? hardnessId,
        Guid stoneClampRatioId,
        SteelMeshType steelMeshType)
    {
        public Guid Id { get; } = id;
        public Guid ProductionProcessId { get; } = productionProcessId;
        public Guid? HardnessId { get; } = hardnessId;
        public Guid StoneClampRatioId { get; } = stoneClampRatioId;
        public SteelMeshType SteelMeshType { get; } = steelMeshType;
        public IList<NormFactorAssignmentCodeUpsertDto> AssignmentCodes { get; } = [];
    }
}